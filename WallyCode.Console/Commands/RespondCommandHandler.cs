using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;

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
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, options.MemoryRoot);

        if (!RoutedSession.Exists(sessionRoot))
        {
            throw new InvalidOperationException($"No active session at {sessionRoot}.");
        }

        var session = RoutedSession.Load(sessionRoot);
        session.PendingResponses.Add(options.Response.Trim());
        if (session.Status == SessionStatus.Blocked)
        {
            session.Status = SessionStatus.Active;
        }
        session.Save(sessionRoot);

        _logger.Section("WallyCode Respond");
        _logger.Success("Response saved for the next loop iteration.");
        return Task.FromResult(0);
    }
}
