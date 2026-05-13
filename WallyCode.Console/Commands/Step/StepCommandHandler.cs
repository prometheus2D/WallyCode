using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class StepCommandHandler
{
    private const string DefaultStepName = "ask";
    private const string EmptySummaryMessage = "[no summary provided]";

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public StepCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(StepCommandOptions options, CancellationToken cancellationToken)
    {
        var requestedPrompt = options.GetRequestedPrompt();
        if (string.IsNullOrWhiteSpace(requestedPrompt))
        {
            throw new InvalidOperationException("Prompt is required. Use: step <prompt> [step]");
        }

        var (projectRoot, settings) = ProjectSettings.ResolveInitializedProjectContext(options.SourcePath);
        var sessionRoot = ProjectSettings.ResolveSessionRoot(settings, projectRoot, options.MemoryRoot);

        var loggingMode = new LoggingMode
        {
            Enabled = options.Log || settings.Logging.Enabled,
            Verbose = options.Verbose || settings.Logging.Verbose
        };
        _logger.ConfigureLogging(sessionRoot, loggingMode);
        _logger.LogAction("Resolved paths", $"projectRoot={projectRoot}; sessionRoot={sessionRoot}");

        _logger.Section("WallyCode Step");

        var stepName = options.GetRequestedStepName()?.Trim() ?? DefaultStepName;
        var definition = WorkflowDefinition.LoadStepByName(stepName);
        var step = definition.GetStep(definition.StartStepName);

        var providerName = string.IsNullOrWhiteSpace(options.Provider) ? settings.Provider : options.Provider!.Trim();
        ILlmProvider? provider = null;
        var model = string.IsNullOrWhiteSpace(options.Model)
            ? settings.Model
            : options.Model!.Trim();
        if (string.Equals(step.ExecutionKind, StepExecutionKind.Provider, StringComparison.OrdinalIgnoreCase))
        {
            provider = _providerRegistry.Get(providerName);
            model = string.IsNullOrWhiteSpace(model) ? provider.DefaultModel : model;
        }

        var session = Session.Start(definition, requestedPrompt!, provider?.Name ?? providerName, model, projectRoot);
        if (Session.Exists(sessionRoot))
        {
            var existingSession = Session.Load(sessionRoot);
            session.Memory = new Dictionary<string, string>(existingSession.Memory, StringComparer.Ordinal);
            _logger.LogAction("Loaded existing memory", $"keys={string.Join(",", session.Memory.Keys)}", verboseOnly: true);
        }

        _logger.Info($"Step: {step.Name}");
        _logger.Info($"Session root: {sessionRoot}");

        var executor = ResolveExecutor(step, provider);
        if (string.Equals(step.ExecutionKind, StepExecutionKind.Provider, StringComparison.OrdinalIgnoreCase))
        {
            await provider!.EnsureReadyAsync(cancellationToken);
            _logger.LogAction("Provider ready", $"provider={provider.Name}; model={model ?? "<default>"}", verboseOnly: true);
        }

        var result = await executor.ExecuteAsync(new StepExecutionContext
        {
            Definition = definition,
            Step = step,
            Session = session,
            SessionRoot = sessionRoot,
            GlobalPrompt = settings.GlobalPrompt ?? string.Empty
        }, cancellationToken);

        result = FilterMemoryUpdates(step, result);

        _logger.Info($"Selected step: {FormatSummary(result.SelectedStep)}");
        _logger.Info($"Summary: {FormatSummary(result.Summary)}");
        if (result.MemoryUpdates.Count > 0)
        {
            _logger.Info($"Memory updates: {string.Join(", ", result.MemoryUpdates.Keys)}");
        }

        _logger.Success("Step run complete.");
        _logger.LogAction("Invocation completed", $"step={step.Name}; selectedStep={result.SelectedStep ?? string.Empty}");
        return 0;
    }

    private IStepExecutor ResolveExecutor(WorkflowStep step, ILlmProvider? provider)
    {
        if (string.Equals(step.ExecutionKind, StepExecutionKind.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return provider is null
                ? throw new InvalidOperationException("Provider step execution requires a provider.")
                : new ProviderStepExecutor(provider, _logger);
        }

        if (string.Equals(step.ExecutionKind, StepExecutionKind.Script, StringComparison.OrdinalIgnoreCase))
        {
            return new ScriptStepExecutor(_logger);
        }

        throw new InvalidOperationException($"No step executor is registered for executionKind '{step.ExecutionKind}'.");
    }

    private StepExecutionResult FilterMemoryUpdates(WorkflowStep step, StepExecutionResult executionResult)
    {
        if (executionResult.MemoryUpdates.Count == 0)
        {
            return executionResult;
        }

        var allowed = new HashSet<string>(step.WritesMemory, StringComparer.Ordinal);
        var filtered = executionResult.MemoryUpdates
            .Where(update => allowed.Contains(update.Key))
            .ToDictionary(update => update.Key, update => update.Value, StringComparer.Ordinal);

        var ignored = executionResult.MemoryUpdates.Keys.Where(key => !allowed.Contains(key)).ToList();
        if (ignored.Count > 0)
        {
            _logger.LogAction("Ignored undeclared memory updates", $"step={step.Name}; keys={string.Join(",", ignored)}");
        }

        return new StepExecutionResult
        {
            SelectedStep = executionResult.SelectedStep,
            Summary = executionResult.Summary,
            MemoryUpdates = filtered
        };
    }

    private static string FormatSummary(string? summary)
    {
        return string.IsNullOrWhiteSpace(summary)
            ? EmptySummaryMessage
            : summary;
    }
}