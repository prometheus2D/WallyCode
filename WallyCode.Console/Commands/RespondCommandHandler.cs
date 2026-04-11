using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class RespondCommandHandler
{
    private readonly AppLogger _logger;

    public RespondCommandHandler(AppLogger logger)
    {
        _logger = logger;
    }

    public Task<int> ExecuteAsync(RespondCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
        var workspace = MemoryWorkspace.Open(projectRoot, commandOptions.MemoryRoot);
        var session = workspace.TryLoadSession()
            ?? throw new InvalidOperationException("No active loop session was found for the selected workspace.");

        workspace.AppendUserResponse(commandOptions.Response);

        _logger.Section("WallyCode Respond");
        _logger.Info($"Initialized source: {projectRoot}");
        _logger.Info($"Initialized memory root: {workspace.RootPath}");
        _logger.Info($"Session file: {workspace.SessionStateFilePath}");
        _logger.Info($"Memory root: {workspace.RootPath}");
        _logger.Success("User response saved for the next loop iteration.");
        return Task.FromResult(0);
    }
}
