using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class UninstallCommandHandler
{
    private readonly AppLogger _logger;
    private readonly string _appDirectoryPath;

    public UninstallCommandHandler(AppLogger logger, string? appDirectoryPath = null)
    {
        _logger = logger;
        _appDirectoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppContext.BaseDirectory
            : appDirectoryPath);
    }

    public Task<int> ExecuteAsync(UninstallCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var installRoot = ResolveTargetDirectory(options);

        _logger.Section("WallyCode Uninstall");
        _logger.Info($"Uninstall target: {installRoot}");

        var removeWorkspaceState = PathEquals(installRoot, _appDirectoryPath);
        var cleaner = new InstallArtifactCleaner(_logger, _appDirectoryPath);
        var result = cleaner.Remove(
            installRoot,
            new InstallRemovalOptions
            {
                IncludeWorkspaceState = removeWorkspaceState,
                AllowRunningAppRemoval = true
            });

        if (!result.RemovedAny)
        {
            _logger.Info("No installed WallyCode artifacts found.");
        }

        return Task.FromResult(0);
    }

    private string ResolveTargetDirectory(UninstallCommandOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SourcePath))
        {
            try
            {
                return Path.GetFullPath(options.SourcePath.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException(
                    $"Invalid uninstall target '{options.SourcePath}'. {exception.Message}",
                    exception);
            }
        }

        if (options.VsBuild)
        {
            return WorkspacePathResolver.ResolveVsBuildWorkspaceRoot(_appDirectoryPath);
        }

        return ProjectSettings.ResolveProjectRoot(null);
    }

    private static bool PathEquals(string leftPath, string rightPath)
    {
        return string.Equals(Path.GetFullPath(leftPath), Path.GetFullPath(rightPath), StringComparison.OrdinalIgnoreCase);
    }
}
