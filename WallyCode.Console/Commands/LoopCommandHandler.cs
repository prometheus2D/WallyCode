using WallyCode.ConsoleApp.App;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Loop;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class LoopCommandHandler
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public LoopCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(LoopCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        if (commandOptions.Steps <= 0)
        {
            throw new InvalidOperationException("The step count must be greater than zero.");
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
        var goal = commandOptions.Goal?.Trim();
        var resolvedMemoryRoot = string.IsNullOrWhiteSpace(commandOptions.MemoryRoot)
            ? null
            : Path.GetFullPath(commandOptions.MemoryRoot);
        var workspace = MemoryWorkspace.Open(projectRoot, resolvedMemoryRoot);
        var session = workspace.TryLoadSession();
        ILlmProvider provider;
        AppOptions options;
        string startupMessage;

        if (session is null)
        {
            if (string.IsNullOrWhiteSpace(goal))
            {
                throw new InvalidOperationException("No active loop session was found. Start one with: loop <goal>");
            }

            var settings = ProjectSettings.Load(projectRoot);
            var providerName = string.IsNullOrWhiteSpace(commandOptions.Provider)
                ? settings.Provider
                : commandOptions.Provider.Trim();
            var template = LoopTemplateRegistry.Load(commandOptions.Template);

            provider = _providerRegistry.Get(providerName);
            options = new AppOptions
            {
                Goal = goal,
                ProviderName = provider.Name,
                Model = string.IsNullOrWhiteSpace(commandOptions.Model)
                    ? (string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model)
                    : commandOptions.Model.Trim(),
                SourcePath = projectRoot,
                MaxIterations = commandOptions.Steps,
                LoopTemplateId = template.TemplateId
            };

            session = workspace.StartNewSession(options, template);
            startupMessage = $"Starting a new loop session with template '{template.TemplateId}'.";
        }
        else
        {
            ValidateExistingSession(commandOptions, goal, projectRoot, workspace, session);
            provider = _providerRegistry.Get(session.ProviderName);
            options = new AppOptions
            {
                Goal = session.Goal,
                ProviderName = session.ProviderName,
                Model = session.Model,
                SourcePath = session.SourcePath,
                MaxIterations = commandOptions.Steps,
                LoopTemplateId = session.LoopTemplateId
            };
            startupMessage = string.IsNullOrWhiteSpace(goal)
                ? $"Resuming active loop session at iteration {session.NextIteration}."
                : $"Resuming existing loop session at iteration {session.NextIteration}.";
        }

        _logger.LogFilePath = workspace.SessionLogFilePath;
        _logger.Section("WallyCode Loop");
        _logger.Info($"Session file: {workspace.SessionStateFilePath}");
        _logger.Info(startupMessage);
        _logger.Info($"Provider: {provider.Name}");
        _logger.Info($"Model: {options.Model ?? provider.DefaultModel}");
        _logger.Info($"Goal: {options.Goal}");
        _logger.Info($"Loop template: {options.LoopTemplateId}");
        _logger.Info($"Memory root: {workspace.RootPath}");
        _logger.Info($"Project root: {options.SourcePath}");

        if (session.IsDone)
        {
            _logger.Success("The active loop session is already complete.");

            if (!string.IsNullOrWhiteSpace(session.DoneReason))
            {
                _logger.Info($"Done reason: {session.DoneReason}");
            }

            return 0;
        }

        await provider.EnsureReadyAsync(cancellationToken);

        var runner = new LoopRunner(provider, _logger);
        await runner.RunAsync(options, workspace, session, cancellationToken);

        _logger.Success("Run complete.");
        return 0;
    }

    private static void ValidateExistingSession(
        LoopCommandOptions commandOptions,
        string? requestedGoal,
        string projectRoot,
        MemoryWorkspace workspace,
        LoopSessionState session)
    {
        if (!string.IsNullOrWhiteSpace(requestedGoal)
            && !string.Equals(requestedGoal, session.Goal, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"An active loop session already exists at {workspace.RootPath} for a different goal. Run loop with no goal to continue it, or choose a different --memory-root.");
        }

        if (!string.Equals(projectRoot, session.SourcePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The active loop session was started for {session.SourcePath}. Use the same --source path or choose a different --memory-root.");
        }

        if (!string.IsNullOrWhiteSpace(commandOptions.Provider)
            && !string.Equals(commandOptions.Provider.Trim(), session.ProviderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The active loop session is using provider {session.ProviderName}. Continue it without changing the provider, or start a different session with another --memory-root.");
        }

        if (!string.IsNullOrWhiteSpace(commandOptions.Model)
            && !string.Equals(commandOptions.Model.Trim(), session.Model, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The active loop session is using model {session.Model}. Continue it without changing the model, or start a different session with another --memory-root.");
        }

        if (!string.IsNullOrWhiteSpace(commandOptions.Template)
            && !string.Equals(commandOptions.Template.Trim(), session.LoopTemplateId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The active loop session is using template {session.LoopTemplateId}. Continue it without changing the template, or start a different session with another --memory-root.");
        }
    }
}