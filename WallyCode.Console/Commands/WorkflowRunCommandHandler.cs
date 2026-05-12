using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class WorkflowRunCommandHandler
{
    private const string DefaultWorkflowName = "requirements";
    private const string EmptySummaryMessage = "[no summary provided]";

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public WorkflowRunCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(RunCommandOptions options, CancellationToken cancellationToken)
    {
        var requestedPrompt = options.GetRequestedPrompt();
        var maxRunIterations = options.ResolveMaxRunIterations();
        if (maxRunIterations <= 0)
        {
            throw new InvalidOperationException("Max run iterations must be greater than zero.");
        }

        if (options.MaxTotalIterations < 0)
        {
            throw new InvalidOperationException("Max total iterations must be zero (no limit) or greater.");
        }

        if (options.MaxStepRepeats < 0)
        {
            throw new InvalidOperationException("Max step repeats must be zero (no limit) or greater.");
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, options.MemoryRoot);
        Directory.CreateDirectory(sessionRoot);

        var loggingMode = new LoggingMode
        {
            Enabled = options.Log || settings.Logging.Enabled,
            Verbose = options.Verbose || settings.Logging.Verbose
        };
        _logger.ConfigureLogging(sessionRoot, loggingMode);
        _logger.LogAction("Resolved paths", $"projectRoot={projectRoot}; sessionRoot={sessionRoot}");

        _logger.Section("WallyCode Run");

        Session session;
        WorkflowDefinition definition;
        ILlmProvider provider;

        if (Session.Exists(sessionRoot))
        {
            session = Session.Load(sessionRoot);
            _logger.LogAction("Loaded session", $"workflow={session.WorkflowName}; status={session.Status}; iteration={session.IterationCount}");
            var workflowName = options.GetRequestedWorkflowName()?.Trim();
            workflowName = string.IsNullOrWhiteSpace(workflowName)
                ? session.WorkflowName
                : WorkflowDefinition.NormalizeWorkflowName(workflowName);
            if (workflowName != session.WorkflowName)
            {
                throw new InvalidOperationException(
                    $"Active session is for workflow '{session.WorkflowName}'. Use --memory-root for a different workflow session.");
            }

            if (Session.IsTerminal(session.Status))
            {
                var archivedPath = Session.ArchiveCompletedSession(sessionRoot);
                _logger.LogAction("Archived terminal session", $"status={session.Status}; archivePath={archivedPath}");
                _logger.Info($"Archived previous {session.Status} session to {archivedPath}.");
                if (session.Status == SessionStatus.Error && !string.IsNullOrWhiteSpace(session.LastSummary))
                {
                    _logger.Warning($"Previous error: {session.LastSummary}");
                }

                if (string.IsNullOrWhiteSpace(requestedPrompt))
                {
                    _logger.Success($"Session is already {session.Status}.");
                    _logger.LogAction("Invocation completed", "Existing terminal session reported without starting a new session.");
                    return 0;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(requestedPrompt))
                {
                    throw new InvalidOperationException(
                        "An active workflow session already exists. Use resume to continue it, or use --memory-root for a different session.");
                }

                definition = WorkflowDefinition.LoadByName(workflowName);
                provider = _providerRegistry.Get(session.ProviderName);
                _logger.LogAction("Resuming session", $"workflow={definition.Name}; provider={provider.Name}; iteration={session.IterationCount}");
                _logger.Info($"Resuming session at iteration {session.IterationCount}.");

                _logger.Info($"Workflow: {definition.Name}");
                _logger.Info($"Start step: {definition.StartStepName}");
                _logger.Info($"Active step: {session.ActiveStepName}");
                _logger.Info($"Status: {session.Status}");
                _logger.Info($"Session root: {sessionRoot}");

                if (session.Status == SessionStatus.Blocked)
                {
                    _logger.Warning("Session is blocked. Use 'respond' to provide input.");
                    _logger.LogAction("Invocation completed", "Blocked session detected; awaiting respond command.");
                    return 0;
                }

                await provider.EnsureReadyAsync(cancellationToken);
                _logger.LogAction("Provider ready", $"provider={provider.Name}; model={session.Model ?? "<default>"}", verboseOnly: true);

                var orchestrator = CreateOrchestrator(provider, definition, sessionRoot);
                var results = await RunWithLimitsAsync(orchestrator, sessionRoot, options, cancellationToken);

                LogResults(options, results, session);
                return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(requestedPrompt))
        {
            throw new InvalidOperationException("No active session. Start one with: run <prompt> [workflow]");
        }

        var providerName = string.IsNullOrWhiteSpace(options.Provider) ? settings.Provider : options.Provider!.Trim();
        provider = _providerRegistry.Get(providerName);
        var model = string.IsNullOrWhiteSpace(options.Model)
            ? (string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model)
            : options.Model!.Trim();

        definition = WorkflowDefinition.LoadByName(options.GetRequestedWorkflowName()?.Trim() ?? DefaultWorkflowName);
        session = Session.Start(definition, requestedPrompt!, provider.Name, model, projectRoot);
        session.Save(sessionRoot);
        _logger.LogAction("Started session", $"workflow={definition.Name}; provider={provider.Name}; model={model ?? "<default>"}; prompt={session.Goal}");
        _logger.Info($"Started session for workflow '{definition.Name}'.");

        _logger.Info($"Workflow: {definition.Name}");
        _logger.Info($"Start step: {definition.StartStepName}");
        _logger.Info($"Active step: {session.ActiveStepName}");
        _logger.Info($"Status: {session.Status}");
        _logger.Info($"Session root: {sessionRoot}");

        await provider.EnsureReadyAsync(cancellationToken);
        _logger.LogAction("Provider ready", $"provider={provider.Name}; model={model ?? "<default>"}", verboseOnly: true);

        var orchestratorNew = CreateOrchestrator(provider, definition, sessionRoot);
        var resultsNew = await RunWithLimitsAsync(orchestratorNew, sessionRoot, options, cancellationToken);

        LogResults(options, resultsNew, session);
        return 0;
    }

    private void LogResults(RunCommandOptions options, IReadOnlyList<IterationResult> results, Session session)
    {
        foreach (var result in results)
        {
            _logger.Section($"Iteration {result.IterationNumber}");
            _logger.Info($"Selected step: {result.SelectedStep}");
            _logger.Info($"Summary: {FormatSummary(result.Summary)}");
            _logger.Info($"Next step: {result.ActiveStepName}");
            _logger.Info($"Status: {result.Status}");
        }

        var finalResult = results.LastOrDefault();
        if (finalResult?.Status == SessionStatus.Error && !string.IsNullOrWhiteSpace(finalResult.Summary))
        {
            _logger.Warning($"Error: {finalResult.Summary}");
        }

        var maxRunIterations = options.ResolveMaxRunIterations();
        if (results.Count >= maxRunIterations && finalResult?.StopsInvocation != true)
        {
            _logger.Warning($"Reached max run iteration limit ({maxRunIterations}). Run resume --max-run-iterations <n> to continue.");
        }

        _logger.Success($"Run complete after {results.Count} iteration(s).");
        _logger.LogAction("Invocation completed", $"iterations={results.Count}; finalStatus={finalResult?.Status ?? session.Status}");
    }

    private static string FormatSummary(string? summary)
    {
        return string.IsNullOrWhiteSpace(summary)
            ? EmptySummaryMessage
            : summary;
    }

    private WorkflowOrchestrator CreateOrchestrator(ILlmProvider provider, WorkflowDefinition definition, string sessionRoot)
    {
        return new WorkflowOrchestrator(
            definition,
            sessionRoot,
            [new ProviderStepExecutor(provider, _logger), new ScriptStepExecutor(_logger)],
            _logger);
    }

    private async Task<IReadOnlyList<IterationResult>> RunWithLimitsAsync(
        WorkflowOrchestrator orchestrator,
        string sessionRoot,
        RunCommandOptions options,
        CancellationToken cancellationToken)
    {
        var maxRunIterations = options.ResolveMaxRunIterations();
        var maxTotalIterations = options.MaxTotalIterations;
        var maxStepRepeats = options.MaxStepRepeats;
        var results = new List<IterationResult>();
        var stepExecutions = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < maxRunIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = Session.Load(sessionRoot);
            if (maxTotalIterations > 0 && session.IterationCount >= maxTotalIterations)
            {
                _logger.Warning($"Reached max total iteration limit ({maxTotalIterations}) for this session.");
                break;
            }

            if (maxStepRepeats > 0)
            {
                var activeStepName = session.ActiveStepName;
                stepExecutions.TryGetValue(activeStepName, out var count);
                count++;
                if (count > maxStepRepeats)
                {
                    _logger.Warning($"Stopped before running step '{activeStepName}' because max step repeats ({maxStepRepeats}) was reached for this invocation.");
                    break;
                }

                stepExecutions[activeStepName] = count;
            }

            var result = await orchestrator.RunOnceAsync(cancellationToken);
            results.Add(result);
            if (result.StopsInvocation)
            {
                break;
            }
        }

        return results;
    }
}