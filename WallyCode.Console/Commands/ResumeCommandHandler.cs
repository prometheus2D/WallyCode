using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ResumeCommandHandler
{
    private readonly LoopCommandHandler _loopCommandHandler;

    public ResumeCommandHandler(LoopCommandHandler loopCommandHandler)
    {
        _loopCommandHandler = loopCommandHandler;
    }

    public async Task<int> ExecuteAsync(ResumeCommandOptions options, CancellationToken cancellationToken)
    {
        var effectiveSteps = options.GetEffectiveSteps();
        if (effectiveSteps <= 0)
        {
            throw new InvalidOperationException("Steps must be greater than zero.");
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, options.MemoryRoot);

        if (!Session.Exists(sessionRoot))
        {
            throw new InvalidOperationException(
                $"No resumable session exists at {Session.FilePath(sessionRoot)}. Start one with: loop <goal>");
        }

        var session = Session.Load(sessionRoot);
        if (session.Status == SessionStatus.Blocked)
        {
            throw new InvalidOperationException("Session is waiting for user input. Use 'respond' before 'resume'.");
        }

        if (Session.IsTerminal(session.Status))
        {
            throw new InvalidOperationException(
                $"Session is terminal with status '{session.Status}' and cannot be resumed. Start a new session with: loop <goal>");
        }

        return await _loopCommandHandler.ExecuteAsync(options.ToLoopOptions(), cancellationToken);
    }
}
