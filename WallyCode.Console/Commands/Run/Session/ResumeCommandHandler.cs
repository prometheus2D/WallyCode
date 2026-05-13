using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ResumeCommandHandler
{
    private readonly WorkflowRunCommandHandler _runCommandHandler;

    public ResumeCommandHandler(WorkflowRunCommandHandler runCommandHandler)
    {
        _runCommandHandler = runCommandHandler;
    }

    public async Task<int> ExecuteAsync(ResumeCommandOptions options, CancellationToken cancellationToken)
    {
        if (options.MaxRunIterations <= 0)
        {
            throw new InvalidOperationException("Max run iterations must be greater than zero.");
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, options.MemoryRoot);

        if (!Session.Exists(sessionRoot))
        {
            throw new InvalidOperationException(
                $"No resumable session exists at {Session.FilePath(sessionRoot)}. Start one with: run <prompt> [workflow]");
        }

        var session = Session.Load(sessionRoot);
        if (session.Status == SessionStatus.Blocked)
        {
            throw new InvalidOperationException("Session is waiting for user input. Use 'respond' before 'resume'.");
        }

        if (Session.IsTerminal(session.Status))
        {
            throw new InvalidOperationException(
                $"Session is terminal with status '{session.Status}' and cannot be resumed. Start a new session with: run <prompt> [workflow]");
        }

        return await _runCommandHandler.ExecuteAsync(options.ToRunOptions(), cancellationToken);
    }
}
