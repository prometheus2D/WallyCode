using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class LoopCommandHandler
{
    private const string DefaultDefinitionName = "requirements";
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
        _logger.LogAction("Resolved paths", $"projectRoot={projectRoot}; sessionRoot={sessionRoot}");

        _logger.Section("WallyCode Loop");

        RoutedSession session;
        RoutingDefinition definition;
        ILlmProvider provider;

        if (RoutedSession.Exists(sessionRoot))
        {
            session = RoutedSession.Load(sessionRoot);
            _logger.LogAction("Loaded session", $"definition={session.DefinitionName}; status={session.Status}; iteration={session.IterationCount}");
            var definitionName = options.Definition?.Trim() ?? session.DefinitionName;
            if (definitionName != session.DefinitionName)
            {
                throw new InvalidOperationException(
                    $"Active session uses definition '{session.DefinitionName}'. Use --memory-root for a different one.");
            }

            if (RoutedSession.IsTerminal(session.Status))
            {
                var archivedPath = RoutedSession.ArchiveCompletedSession(sessionRoot);
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
                definition = RoutingDefinition.LoadByName(definitionName);
                provider = _providerRegistry.Get(session.ProviderName);
                _logger.LogAction("Resuming session", $"definition={definition.Name}; provider={provider.Name}; iteration={session.IterationCount}");
                _logger.Info($"Resuming session at iteration {session.IterationCount}.");

                _logger.Info($"Definition: {definition.Name}");
                _logger.Info($"Active unit: {session.ActiveUnitName}");
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

                var runner = new RoutedRunner(provider, definition, sessionRoot, _logger);
                var results = await runner.RunAsync(options.Steps, cancellationToken);

                foreach (var r in results)
                {
                    _logger.Section($"Iteration {r.IterationNumber}");
                    _logger.Info($"Selected keyword: {r.SelectedKeyword}");
                    _logger.Info($"Summary: {FormatSummary(r.Summary)}");
                    _logger.Info($"Next unit: {r.ActiveUnitName}");
                    _logger.Info($"Status: {r.Status}");
                }

                var finalResult = results.LastOrDefault();
                if (finalResult?.Status == SessionStatus.Error && !string.IsNullOrWhiteSpace(finalResult.Summary))
                {
                    _logger.Warning($"Error: {finalResult.Summary}");
                }

                _logger.Success($"Run complete after {results.Count} iteration(s).");
                _logger.LogAction("Invocation completed", $"iterations={results.Count}; finalStatus={finalResult?.Status ?? session.Status}");
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
        _logger.LogAction("Started session", $"definition={definition.Name}; provider={provider.Name}; model={model ?? "<default>"}; goal={session.Goal}");
        _logger.Info($"Started session on definition '{definition.Name}'.");

        _logger.Info($"Definition: {definition.Name}");
        _logger.Info($"Active unit: {session.ActiveUnitName}");
        _logger.Info($"Status: {session.Status}");
        _logger.Info($"Session root: {sessionRoot}");

        await provider.EnsureReadyAsync(cancellationToken);
        _logger.LogAction("Provider ready", $"provider={provider.Name}; model={model ?? "<default>"}", verboseOnly: true);

        var runnerNew = new RoutedRunner(provider, definition, sessionRoot, _logger);
        var resultsNew = await runnerNew.RunAsync(options.Steps, cancellationToken);

        foreach (var r in resultsNew)
        {
            _logger.Section($"Iteration {r.IterationNumber}");
            _logger.Info($"Selected keyword: {r.SelectedKeyword}");
            _logger.Info($"Summary: {FormatSummary(r.Summary)}");
            _logger.Info($"Next unit: {r.ActiveUnitName}");
            _logger.Info($"Status: {r.Status}");
        }

        var finalNewResult = resultsNew.LastOrDefault();
        if (finalNewResult?.Status == SessionStatus.Error && !string.IsNullOrWhiteSpace(finalNewResult.Summary))
        {
            _logger.Warning($"Error: {finalNewResult.Summary}");
        }

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
}
