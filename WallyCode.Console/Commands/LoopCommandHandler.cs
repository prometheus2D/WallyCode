using WallyCode.ConsoleApp.App;
using WallyCode.ConsoleApp.Copilot;
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

    public Task<int> ExecuteAsync(LoopCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        return ExecuteHandledAsync(async () =>
        {
            var requestedSteps = ValidateRequestedSteps(commandOptions.Steps);
            var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
            var goal = commandOptions.Goal?.Trim();
            var workspace = OpenWorkspace(projectRoot, commandOptions.MemoryRoot);
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

                provider = _providerRegistry.Get(providerName);

                options = new AppOptions
                {
                    Goal = goal,
                    ProviderName = provider.Name,
                    Model = string.IsNullOrWhiteSpace(commandOptions.Model)
                        ? provider.DefaultModel
                        : commandOptions.Model.Trim(),
                    SourcePath = projectRoot,
                    MaxIterations = requestedSteps
                };

                session = workspace.StartNewSession(options);
                startupMessage = "Starting a new loop session.";
            }
            else
            {
                ValidateExistingSession(commandOptions, goal, projectRoot, workspace, session);
                provider = _providerRegistry.Get(session.ProviderName);
                options = CreateOptions(session, requestedSteps);
                startupMessage = string.IsNullOrWhiteSpace(goal)
                    ? $"Resuming active loop session at iteration {session.NextIteration}."
                    : $"Resuming existing loop session at iteration {session.NextIteration}.";
            }

            return await ExecuteSessionAsync(options, workspace, session, provider, startupMessage, cancellationToken);
        });
    }

    private async Task<int> ExecuteHandledAsync(Func<Task<int>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Run cancelled.");
            return 2;
        }
        catch (Exception exception)
        {
            _logger.Error(exception.ToString());
            return 1;
        }
    }

    private async Task<int> ExecuteSessionAsync(
        AppOptions options,
        MemoryWorkspace workspace,
        LoopSessionState session,
        ILlmProvider provider,
        string startupMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogFilePath = workspace.SessionLogFilePath;
        _logger.Section("WallyCode Loop");
        _logger.Info($"Session file: {workspace.SessionStateFilePath}");
        _logger.Info(startupMessage);
        _logger.Info($"Provider: {provider.Name}");
        _logger.Info($"Model: {options.Model ?? provider.DefaultModel}");
        _logger.Info($"Goal: {options.Goal}");
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

    private static int ValidateRequestedSteps(int requestedSteps)
    {
        if (requestedSteps <= 0)
        {
            throw new InvalidOperationException("The step count must be greater than zero.");
        }

        return requestedSteps;
    }

    private static AppOptions CreateOptions(LoopSessionState session, int requestedSteps)
    {
        return new AppOptions
        {
            Goal = session.Goal,
            ProviderName = session.ProviderName,
            Model = session.Model,
            SourcePath = session.SourcePath,
            MaxIterations = requestedSteps
        };
    }

    private static MemoryWorkspace OpenWorkspace(string projectRoot, string? memoryRoot)
    {
        var resolvedMemoryRoot = string.IsNullOrWhiteSpace(memoryRoot)
            ? null
            : Path.GetFullPath(memoryRoot);

        return MemoryWorkspace.Open(projectRoot, resolvedMemoryRoot);
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
    }
}