using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class RespondCommandHandler
{
    private readonly AppLogger _logger;

    public RespondCommandHandler(AppLogger logger)
    {
        _logger = logger;
    }

    public Task<int> ExecuteAsync(RespondCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.Response))
        {
            throw new InvalidOperationException("A non-empty response is required.");
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, options.MemoryRoot);
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
        var response = options.Response.Trim();
        session.PendingResponses.Add(response);
        if (session.Status == SessionStatus.Blocked)
        {
            session.Status = SessionStatus.Active;
        }
        session.Save(sessionRoot);

        _logger.LogAction("Saved response", $"status={session.Status}; pendingResponses={session.PendingResponses.Count}");
        _logger.LogExchange("USER", "respond", response);
        _logger.Section("WallyCode Respond");
        _logger.Success("Response saved for the next loop iteration.");
        return Task.FromResult(0);
    }
}
