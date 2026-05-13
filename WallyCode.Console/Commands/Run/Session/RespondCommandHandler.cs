using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class RespondCommandHandler
{
    private readonly WorkflowRunCommandHandler _runCommandHandler;
    private readonly AppLogger _logger;

    public RespondCommandHandler(WorkflowRunCommandHandler runCommandHandler, AppLogger logger)
    {
        _runCommandHandler = runCommandHandler;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(RespondCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var responseText = options.ResolveResponse();

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("A non-empty response is required.");
        }

        var (projectRoot, settings) = ProjectSettings.ResolveProjectContext(options.SourcePath);
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
        if (Session.IsTerminal(session.Status))
        {
            throw new InvalidOperationException($"Session is terminal with status '{session.Status}' and cannot accept a response.");
        }

        var response = responseText.Trim();
        session.PendingResponses.Add(response);
        if (session.Status == SessionStatus.Blocked)
        {
            session.Status = SessionStatus.Active;
        }
        session.Save(sessionRoot);

        _logger.LogAction("Saved response", $"status={session.Status}; pendingResponses={session.PendingResponses.Count}");
        _logger.LogExchange("USER", "respond", response);
        _logger.Section("WallyCode Respond");
        _logger.Success("Response saved. Resuming workflow.");
        return await _runCommandHandler.ExecuteAsync(options.ToRunOptions(), cancellationToken);
    }
}
