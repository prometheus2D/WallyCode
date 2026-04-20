using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class LoopCommandHandler
{
    private const string DefaultDefinitionName = "requirements";

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public LoopCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(LoopCommandOptions options, CancellationToken cancellationToken)
    {
        if (options.Steps <= 0)
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

        _logger.Section("WallyCode Loop");

        RoutedSession session;
        RoutingDefinition definition;
        ILlmProvider provider;

        if (RoutedSession.Exists(sessionRoot))
        {
            session = RoutedSession.Load(sessionRoot);
            var definitionName = options.Definition?.Trim() ?? session.DefinitionName;
            if (definitionName != session.DefinitionName)
            {
                throw new InvalidOperationException(
                    $"Active session uses definition '{session.DefinitionName}'. Use --memory-root for a different one.");
            }

            if (RoutedSession.IsTerminal(session.Status))
            {
                var archivedPath = RoutedSession.ArchiveCompletedSession(sessionRoot);
                _logger.Info($"Archived previous {session.Status} session to {archivedPath}.");

                if (string.IsNullOrWhiteSpace(options.Goal))
                {
                    _logger.Success($"Session is already {session.Status}.");
                    return 0;
                }
            }
            else
            {
                definition = RoutingDefinition.LoadByName(definitionName);
                provider = _providerRegistry.Get(session.ProviderName);
                _logger.Info($"Resuming session at iteration {session.IterationCount}.");

                _logger.Info($"Definition: {definition.Name}");
                _logger.Info($"Active unit: {session.ActiveUnitName}");
                _logger.Info($"Status: {session.Status}");
                _logger.Info($"Session root: {sessionRoot}");

                if (session.Status == SessionStatus.Blocked)
                {
                    _logger.Warning("Session is blocked. Use 'respond' to provide input.");
                    return 0;
                }

                await provider.EnsureReadyAsync(cancellationToken);

                var runner = new RoutedRunner(provider, definition, sessionRoot, _logger);
                var results = await runner.RunAsync(options.Steps, cancellationToken);

                foreach (var r in results)
                {
                    _logger.Section($"Iteration {r.IterationNumber}");
                    _logger.Info($"Selected keyword: {r.SelectedKeyword}");
                    if (!string.IsNullOrWhiteSpace(r.Summary)) _logger.Info($"Summary: {r.Summary}");
                    _logger.Info($"Next unit: {r.ActiveUnitName}");
                    _logger.Info($"Status: {r.Status}");
                }

                _logger.Success($"Run complete after {results.Count} iteration(s).");
                return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(options.Goal))
        {
            throw new InvalidOperationException("No active session. Start one with: loop <goal> [--definition <name>]");
        }

        var providerName = string.IsNullOrWhiteSpace(options.Provider) ? settings.Provider : options.Provider!.Trim();
        provider = _providerRegistry.Get(providerName);
        var model = string.IsNullOrWhiteSpace(options.Model)
            ? (string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model)
            : options.Model!.Trim();

        definition = RoutingDefinition.LoadByName(options.Definition?.Trim() ?? DefaultDefinitionName);
        session = RoutedSession.Start(definition, options.Goal!, provider.Name, model, projectRoot);
        session.Save(sessionRoot);
        _logger.Info($"Started session on definition '{definition.Name}'.");

        _logger.Info($"Definition: {definition.Name}");
        _logger.Info($"Active unit: {session.ActiveUnitName}");
        _logger.Info($"Status: {session.Status}");
        _logger.Info($"Session root: {sessionRoot}");

        await provider.EnsureReadyAsync(cancellationToken);

        var newRunner = new RoutedRunner(provider, definition, sessionRoot, _logger);
        var newResults = await newRunner.RunAsync(options.Steps, cancellationToken);

        foreach (var r in newResults)
        {
            _logger.Section($"Iteration {r.IterationNumber}");
            _logger.Info($"Selected keyword: {r.SelectedKeyword}");
            if (!string.IsNullOrWhiteSpace(r.Summary)) _logger.Info($"Summary: {r.Summary}");
            _logger.Info($"Next unit: {r.ActiveUnitName}");
            _logger.Info($"Status: {r.Status}");
        }

        _logger.Success($"Run complete after {newResults.Count} iteration(s).");
        return 0;
    }
}
