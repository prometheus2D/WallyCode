using System.Diagnostics;
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
        var deploymentManifest = DeploymentManifest.TryLoad(targetDirectory);

        _logger.Section("WallyCode Cleanup");
        _logger.Info($"Cleanup target: {targetDirectory}");

        var removedAny = false;
        var deferredFiles = new List<string>();
        var deferredDirectories = new List<string>();

        if (deploymentManifest is not null && !options.PreserveDeployedPayload)
        {
            removedAny |= RemoveDeploymentArtifacts(targetDirectory, deploymentManifest, deferredFiles, deferredDirectories);
        }

        if (options.PreserveDeployedPayload)
        {
            removedAny |= RemoveFile(
                ProjectSettings.GetActiveProjectFilePath(targetDirectory),
                "Removed deployed wallycode.active.json.",
                deferredFiles);
        }

        removedAny |= RemoveFile(settingsPath, "Removed wallycode.json.", deferredFiles);

        removedAny |= RemoveDirectory(runtimeRoot, "Removed .wallycode.", deferredDirectories);

        ProjectSettings.ClearActiveProjectPathIfMatches(targetDirectory, _appDirectoryPath);

        if (deferredFiles.Count > 0 || deferredDirectories.Count > 0)
        {
            ScheduleDeferredCleanup(deferredFiles, deferredDirectories);
            _logger.Info("Scheduled removal of in-use deployed artifacts after this process exits.");
            removedAny = true;
        }

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

    private bool RemoveDeploymentArtifacts(
        string targetDirectory,
        DeploymentManifest deploymentManifest,
        List<string> deferredFiles,
        List<string> deferredDirectories)
    {
        var removedAny = false;

        foreach (var filePath in deploymentManifest.ResolveFilePaths(targetDirectory))
        {
            removedAny |= RemoveFile(filePath, $"Removed deployed file: {filePath}", deferredFiles);
        }

        foreach (var directoryPath in deploymentManifest.ResolveDirectoryPaths(targetDirectory)
            .OrderByDescending(path => path.Length)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            removedAny |= RemoveDirectory(directoryPath, $"Removed deployed directory: {directoryPath}", deferredDirectories);
        }

        return removedAny;
    }

    private bool RemoveFile(string filePath, string successMessage, List<string> deferredFiles)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            File.Delete(filePath);
            _logger.Success(successMessage);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            deferredFiles.Add(filePath);
            return true;
        }
    }

    private bool RemoveDirectory(string directoryPath, string successMessage, List<string> deferredDirectories)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);
            _logger.Success(successMessage);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            deferredDirectories.Add(directoryPath);
            return true;
        }
    }

    private void ScheduleDeferredCleanup(IReadOnlyCollection<string> filePaths, IReadOnlyCollection<string> directoryPaths)
    {
        if (filePaths.Count == 0 && directoryPaths.Count == 0)
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            _logger.Warning("Deferred cleanup is only implemented on Windows. Some deployed artifacts may remain until removed manually.");
            return;
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), $"wallycode-cleanup-{Guid.NewGuid():N}.cmd");
        var scriptLines = new List<string>
        {
            "@echo off",
            "setlocal"
        };

        var distinctFiles = filePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var distinctDirectories = directoryPaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => path.Length)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        scriptLines.Add("for /l %%i in (1,1,20) do (");
        foreach (var filePath in distinctFiles)
        {
            var quotedPath = QuoteForCmd(filePath);
            scriptLines.Add($"  if exist {quotedPath} del /f /q {quotedPath} >nul 2>nul");
        }

        foreach (var directoryPath in distinctDirectories)
        {
            var quotedPath = QuoteForCmd(directoryPath);
            scriptLines.Add($"  if exist {quotedPath} rmdir /s /q {quotedPath} >nul 2>nul");
        }

        scriptLines.Add("  set pending=");
        foreach (var filePath in distinctFiles)
        {
            scriptLines.Add($"  if exist {QuoteForCmd(filePath)} set pending=1");
        }

        foreach (var directoryPath in distinctDirectories)
        {
            scriptLines.Add($"  if exist {QuoteForCmd(directoryPath)} set pending=1");
        }

        scriptLines.Add("  if not defined pending goto done");
        scriptLines.Add("  timeout /t 1 /nobreak >nul");
        scriptLines.Add(")");
        scriptLines.Add(":done");
        scriptLines.Add("del /f /q \"%~f0\" >nul 2>nul");

        File.WriteAllLines(scriptPath, scriptLines);

        using var cleanupProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/d /c \"\"{scriptPath}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory
        });
    }

    private static string QuoteForCmd(string path)
    {
        return $"\"{path}\"";
    }
}
