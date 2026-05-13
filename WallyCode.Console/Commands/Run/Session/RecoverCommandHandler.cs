using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class RecoverCommandHandler
{
    private readonly WorkflowRunCommandHandler _runCommandHandler;
    private readonly AppLogger _logger;

    public RecoverCommandHandler(WorkflowRunCommandHandler runCommandHandler, AppLogger logger)
    {
        _runCommandHandler = runCommandHandler;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(RecoverCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var action = options.ResolveAction();
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new InvalidOperationException("A non-empty recovery action is required.");
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

        if (!Session.Exists(sessionRoot))
        {
            throw new InvalidOperationException($"No active session at {sessionRoot}.");
        }

        var session = Session.Load(sessionRoot);
        if (!Session.IsTerminal(session.Status))
        {
            throw new InvalidOperationException($"Session status is '{session.Status}'. Use resume/respond for non-terminal sessions.");
        }

        var archivedPath = Session.ArchiveCompletedSession(sessionRoot);
        _logger.LogAction("Archived terminal session", $"status={session.Status}; archivePath={archivedPath}");

        var runOptions = options.ToRunOptions(session.WorkflowName, session.ProviderName, session.Model);

        _logger.Section("WallyCode Recover");
        _logger.Info($"Recovered from terminal session '{session.Status}'.");
        _logger.Info($"Workflow: {session.WorkflowName}");
        _logger.Info($"Provider: {session.ProviderName}");
        _logger.Success("Recovery action saved. Starting new run.");

        return await _runCommandHandler.ExecuteAsync(runOptions, cancellationToken);
    }
}
