using System.Diagnostics;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class InstallRemovalOptions
{
    public bool IncludeWorkspaceState { get; init; }

    public bool AllowRunningAppRemoval { get; init; }

    public string NoManifestMessage { get; init; } = "No local WallyCode installation found.";
}

internal sealed class InstallRemovalResult
{
    public bool RemovedAny { get; init; }

    public bool DeferredAny { get; init; }

    public bool ManifestFound { get; init; }

    public bool SkippedRunningAppRemoval { get; init; }
}

internal sealed class InstallArtifactCleaner
{
    private const string InstalledExecutableName = "wallycode.exe";

    private readonly AppLogger _logger;
    private readonly string _appDirectoryPath;

    public InstallArtifactCleaner(AppLogger logger, string appDirectoryPath)
    {
        _logger = logger;
        _appDirectoryPath = Path.GetFullPath(appDirectoryPath);
    }

    public InstallRemovalResult Remove(string installRoot, InstallRemovalOptions options)
    {
        installRoot = Path.GetFullPath(installRoot);
        var removingRunningAppDirectory = PathEquals(installRoot, _appDirectoryPath);
        if (removingRunningAppDirectory && !options.AllowRunningAppRemoval)
        {
            _logger.Warning("Install target is the running WallyCode directory. Existing app files were not removed before copying.");
            return new InstallRemovalResult { SkippedRunningAppRemoval = true };
        }

        if (removingRunningAppDirectory)
        {
            _logger.Warning("Uninstall target is the running WallyCode directory. The running application cannot remove itself immediately; locked files will be scheduled for removal after exit.");
        }

        var manifest = InstallManifest.TryLoad(installRoot);
        var filePaths = manifest is null
            ? GetFallbackFilePaths(installRoot)
            : manifest.ResolveFilePaths(installRoot);
        var directoryPaths = manifest is null
            ? GetFallbackDirectoryPaths(installRoot)
            : manifest.ResolveDirectoryPaths(installRoot);

        if (options.IncludeWorkspaceState)
        {
            filePaths = [.. filePaths, ProjectSettings.GetFilePath(installRoot)];
            directoryPaths = [.. directoryPaths, ProjectSettings.ResolveRuntimeRoot(installRoot)];
        }

        var removedAny = false;
        var deferredFiles = new List<string>();
        var deferredDirectories = new List<string>();

        foreach (var filePath in filePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            removedAny |= RemoveFile(filePath, deferredFiles);
        }

        foreach (var directoryPath in directoryPaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => path.Length)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            removedAny |= RemoveDirectory(directoryPath, deferredDirectories);
        }

        var deferredAny = deferredFiles.Count > 0 || deferredDirectories.Count > 0;
        if (deferredAny)
        {
            ScheduleDeferredCleanup(deferredFiles, deferredDirectories);
            _logger.Info("Scheduled removal of in-use WallyCode artifacts after this process exits.");
            removedAny = true;
        }

        if (!removedAny && manifest is null && !options.IncludeWorkspaceState)
        {
            _logger.Info(options.NoManifestMessage);
        }

        return new InstallRemovalResult
        {
            RemovedAny = removedAny,
            DeferredAny = deferredAny,
            ManifestFound = manifest is not null
        };
    }

    private static IReadOnlyList<string> GetFallbackFilePaths(string installRoot)
    {
        List<string> fallbackFilePaths =
        [
            Path.Combine(installRoot, InstalledExecutableName),
            Path.Combine(installRoot, "WallyCode.Console.exe"),
            ProjectSettings.GetActiveProjectFilePath(installRoot),
            InstallManifest.GetFilePath(installRoot)
        ];

        var hasInstallMarker = fallbackFilePaths.Any(File.Exists)
            || Directory.Exists(Path.Combine(installRoot, "Loadables"));
        if (hasInstallMarker && Directory.Exists(installRoot))
        {
            fallbackFilePaths.AddRange(Directory.EnumerateFiles(installRoot, "*", SearchOption.TopDirectoryOnly)
                .Where(IsRuntimeCompanionFile));
        }

        return fallbackFilePaths;
    }

    private static IReadOnlyList<string> GetFallbackDirectoryPaths(string installRoot)
    {
        return [Path.Combine(installRoot, "Loadables")];
    }

    private static bool IsRuntimeCompanionFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase);
    }

    private bool RemoveFile(string filePath, List<string> deferredFiles)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            File.Delete(filePath);
            _logger.Success($"Removed WallyCode file: {filePath}");
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            deferredFiles.Add(filePath);
            return true;
        }
    }

    private bool RemoveDirectory(string directoryPath, List<string> deferredDirectories)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);
            _logger.Success($"Removed WallyCode directory: {directoryPath}");
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
            _logger.Warning("Deferred uninstall cleanup is only implemented on Windows. Some WallyCode artifacts may remain until removed manually.");
            return;
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), $"wallycode-uninstall-{Guid.NewGuid():N}.cmd");
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

    private static bool PathEquals(string leftPath, string rightPath)
    {
        return string.Equals(Path.GetFullPath(leftPath), Path.GetFullPath(rightPath), StringComparison.OrdinalIgnoreCase);
    }

    private static string QuoteForCmd(string path)
    {
        return $"\"{path}\"";
    }
}