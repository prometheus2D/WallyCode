using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class CleanupCommandHandler
{
    private readonly AppLogger _logger;
    private readonly string _appDirectoryPath;

    public CleanupCommandHandler(AppLogger logger, string? appDirectoryPath = null)
    {
        _logger = logger;
        _appDirectoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppContext.BaseDirectory
            : appDirectoryPath);
    }

    public Task<int> ExecuteAsync(CleanupCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targetDirectory = ResolveTargetDirectory(options);
        var settingsPath = ProjectSettings.GetFilePath(targetDirectory);
        var runtimeRoot = ProjectSettings.ResolveRuntimeRoot(targetDirectory);

        _logger.Section("WallyCode Cleanup");
        _logger.Info($"Cleanup target: {targetDirectory}");

        var removedAny = false;

        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
            _logger.Success("Removed wallycode.json.");
            removedAny = true;
        }

        if (Directory.Exists(runtimeRoot))
        {
            Directory.Delete(runtimeRoot, recursive: true);
            _logger.Success("Removed .wallycode.");
            removedAny = true;
        }

        ProjectSettings.ClearActiveProjectPathIfMatches(targetDirectory, _appDirectoryPath);

        if (!removedAny)
        {
            _logger.Info("No WallyCode artifacts found.");
        }

        return Task.FromResult(0);
    }

    private string ResolveTargetDirectory(CleanupCommandOptions options)
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
                    $"Invalid cleanup target '{options.SourcePath}'. {exception.Message}",
                    exception);
            }
        }

        if (options.VsBuild)
        {
            return WorkspacePathResolver.ResolveVsBuildWorkspaceRoot(_appDirectoryPath);
        }

        return ProjectSettings.ResolveProjectRoot(null);
    }
}
