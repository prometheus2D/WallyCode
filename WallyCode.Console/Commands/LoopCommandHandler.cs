using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class LoopCommandHandler
{
    private const string DefaultWorkflowName = "requirements";
    private const string EmptySummaryMessage = "[no summary provided]";

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public LoopCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(LoopCommandOptions options, CancellationToken cancellationToken)
    {
        var effectiveSteps = options.GetEffectiveSteps();
        if (effectiveSteps <= 0)
        {
            throw new InvalidOperationException("Steps must be greater than zero.");
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

        _logger.Section("WallyCode Loop");

        Session session;
        WorkflowDefinition definition;
        ILlmProvider provider;

        if (Session.Exists(sessionRoot))
        {
            session = Session.Load(sessionRoot);
            _logger.LogAction("Loaded session", $"workflow={session.WorkflowName}; status={session.Status}; iteration={session.IterationCount}");
            var workflowName = options.GetRequestedStartStepName()?.Trim();
            workflowName = string.IsNullOrWhiteSpace(workflowName)
                ? session.WorkflowName
                : WorkflowDefinition.NormalizeStartStepName(workflowName);
            if (workflowName != session.WorkflowName)
            {
                throw new InvalidOperationException(
                    $"Active session started from step '{session.WorkflowName}'. Use --memory-root for a different one.");
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

                if (string.IsNullOrWhiteSpace(options.Goal))
                {
                    _logger.Success($"Session is already {session.Status}.");
                    _logger.LogAction("Invocation completed", "Existing terminal session reported without starting a new session.");
                    return 0;
                }
            }
            else
            {
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
                var results = await orchestrator.RunAsync(effectiveSteps, cancellationToken);

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

                WarnIfUntilCompleteHitCap(options, effectiveSteps, results.Count, finalResult);

                _logger.Success($"Run complete after {results.Count} iteration(s).");
                _logger.LogAction("Invocation completed", $"iterations={results.Count}; finalStatus={finalResult?.Status ?? session.Status}");
                return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(options.Goal))
        {
            throw new InvalidOperationException("No active session. Start one with: loop <goal> [--start-step <name>]");
        }

        var providerName = string.IsNullOrWhiteSpace(options.Provider) ? settings.Provider : options.Provider!.Trim();
        provider = _providerRegistry.Get(providerName);
        var model = string.IsNullOrWhiteSpace(options.Model)
            ? (string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model)
            : options.Model!.Trim();

        definition = WorkflowDefinition.LoadByName(options.GetRequestedStartStepName()?.Trim() ?? DefaultWorkflowName);
        session = Session.Start(definition, options.Goal!, provider.Name, model, projectRoot);
        session.Save(sessionRoot);
        _logger.LogAction("Started session", $"startStep={definition.Name}; provider={provider.Name}; model={model ?? "<default>"}; goal={session.Goal}");
        _logger.Info($"Started session for workflow '{definition.Name}'.");

        _logger.Info($"Workflow: {definition.Name}");
        _logger.Info($"Start step: {definition.StartStepName}");
        _logger.Info($"Active step: {session.ActiveStepName}");
        _logger.Info($"Status: {session.Status}");
        _logger.Info($"Session root: {sessionRoot}");

        await provider.EnsureReadyAsync(cancellationToken);
        _logger.LogAction("Provider ready", $"provider={provider.Name}; model={model ?? "<default>"}", verboseOnly: true);

        var orchestratorNew = CreateOrchestrator(provider, definition, sessionRoot);
        var resultsNew = await orchestratorNew.RunAsync(effectiveSteps, cancellationToken);

        foreach (var result in resultsNew)
        {
            _logger.Section($"Iteration {result.IterationNumber}");
            _logger.Info($"Selected step: {result.SelectedStep}");
            _logger.Info($"Summary: {FormatSummary(result.Summary)}");
            _logger.Info($"Next step: {result.ActiveStepName}");
            _logger.Info($"Status: {result.Status}");
        }

        var finalNewResult = resultsNew.LastOrDefault();
        if (finalNewResult?.Status == SessionStatus.Error && !string.IsNullOrWhiteSpace(finalNewResult.Summary))
        {
            _logger.Warning($"Error: {finalNewResult.Summary}");
        }

        WarnIfUntilCompleteHitCap(options, effectiveSteps, resultsNew.Count, finalNewResult);

        _logger.Success($"Run complete after {resultsNew.Count} iteration(s).");
        _logger.LogAction("Invocation completed", $"iterations={resultsNew.Count}; finalStatus={finalNewResult?.Status ?? session.Status}");
        return 0;
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

    private void WarnIfUntilCompleteHitCap(LoopCommandOptions options, int effectiveSteps, int resultCount, IterationResult? finalResult)
    {
        if (options.UntilComplete && resultCount >= effectiveSteps && finalResult?.StopsInvocation != true)
        {
            _logger.Warning($"Reached --until-complete safety cap ({effectiveSteps} iterations). Run resume --until-complete to continue.");
        }
    }
}
