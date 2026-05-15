using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class InstallCommandHandler
{
    private const string InstalledExecutableName = "wallycode.exe";

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;
    private readonly string _appDirectoryPath;

    public InstallCommandHandler(ProviderRegistry providerRegistry, AppLogger logger, string? appDirectoryPath = null)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
        _appDirectoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppContext.BaseDirectory
            : appDirectoryPath);
    }

    public Task<int> ExecuteAsync(InstallCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var installRoot = ResolveTargetDirectory(options.SourcePath, options.VsBuild, "install");
        try
        {
            Directory.CreateDirectory(installRoot);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"Invalid install target '{installRoot}'. {exception.Message}",
                exception);
        }

        _logger.Section("WallyCode Install");
        _logger.Info($"Install target: {installRoot}");

        var sourceExecutablePath = ResolveSourceExecutablePath();
        var installedExecutablePath = Path.Combine(installRoot, InstalledExecutableName);
        var cleaner = new InstallArtifactCleaner(_logger, _appDirectoryPath);

        cleaner.Remove(
            installRoot,
            new InstallRemovalOptions
            {
                NoManifestMessage = "No previous local WallyCode installation found.",
                AllowRunningAppRemoval = false
            });

        CopyFileIfDifferent(sourceExecutablePath, installedExecutablePath);
        var runtimeCompanionPaths = CopyRuntimeCompanionFiles(sourceExecutablePath, installRoot);
        var loadablesPath = CopyLoadables(installRoot);
        var activeProjectPath = ProjectSettings.GetActiveProjectFilePath(installRoot);
        var manifestPath = InstallManifest.GetFilePath(installRoot);

        ProjectSettings.SaveActiveProjectPath(installRoot, installRoot);
        InstallManifest.Save(
            installRoot,
            [installedExecutablePath, .. runtimeCompanionPaths, activeProjectPath, manifestPath],
            loadablesPath is null ? [] : [loadablesPath]);

        _logger.Success($"Install successful: {installedExecutablePath}");
        _logger.Info($"Run the installed executable from its new location: {installedExecutablePath}");

        if (options.Setup)
        {
            var setupHandler = new SetupCommandHandler(_providerRegistry, _logger, installRoot);
            setupHandler.ExecuteAsync(
                new SetupCommandOptions { SourcePath = installRoot, Cleanup = true },
                cancellationToken).GetAwaiter().GetResult();
        }

        WriteNextCommands(installRoot, installedExecutablePath, options.Setup);
        return Task.FromResult(0);
    }

    private string ResolveTargetDirectory(string? sourcePath, bool vsBuild, string commandName)
    {
        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            try
            {
                return Path.GetFullPath(sourcePath.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException(
                    $"Invalid {commandName} target '{sourcePath}'. {exception.Message}",
                    exception);
            }
        }

        if (vsBuild)
        {
            return WorkspacePathResolver.ResolveVsBuildWorkspaceRoot(_appDirectoryPath);
        }

        return _appDirectoryPath;
    }

    private string ResolveSourceExecutablePath()
    {
        var preferredExecutablePath = Path.Combine(_appDirectoryPath, InstalledExecutableName);
        if (File.Exists(preferredExecutablePath))
        {
            return preferredExecutablePath;
        }

        var projectExecutablePath = Path.Combine(_appDirectoryPath, "WallyCode.Console.exe");
        if (File.Exists(projectExecutablePath))
        {
            return projectExecutablePath;
        }

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath)
            && File.Exists(processPath)
            && PathEquals(Path.GetDirectoryName(processPath) ?? string.Empty, _appDirectoryPath))
        {
            return processPath;
        }

        var executablePath = Directory.EnumerateFiles(_appDirectoryPath, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            return executablePath;
        }

        throw new InvalidOperationException($"No WallyCode executable was found in {_appDirectoryPath}.");
    }

    private IReadOnlyList<string> CopyRuntimeCompanionFiles(string sourceExecutablePath, string installRoot)
    {
        var copiedPaths = new List<string>();
        foreach (var sourcePath in Directory.EnumerateFiles(_appDirectoryPath, "*", SearchOption.TopDirectoryOnly))
        {
            if (PathEquals(sourcePath, sourceExecutablePath) || !IsRuntimeCompanionFile(sourcePath))
            {
                continue;
            }

            var targetPath = Path.Combine(installRoot, Path.GetFileName(sourcePath));
            CopyFileIfDifferent(sourcePath, targetPath);
            copiedPaths.Add(targetPath);
        }

        return copiedPaths;
    }

    private string? CopyLoadables(string installRoot)
    {
        var sourceLoadablesPath = Path.Combine(_appDirectoryPath, "Loadables");
        if (!Directory.Exists(sourceLoadablesPath))
        {
            return null;
        }

        var targetLoadablesPath = Path.Combine(installRoot, "Loadables");
        CopyDirectory(sourceLoadablesPath, targetLoadablesPath);
        return targetLoadablesPath;
    }

    private static bool IsRuntimeCompanionFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyDirectory(string sourceDirectoryPath, string targetDirectoryPath)
    {
        if (PathEquals(sourceDirectoryPath, targetDirectoryPath))
        {
            return;
        }

        Directory.CreateDirectory(targetDirectoryPath);
        foreach (var directoryPath in Directory.EnumerateDirectories(sourceDirectoryPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectoryPath, directoryPath);
            Directory.CreateDirectory(Path.Combine(targetDirectoryPath, relativePath));
        }

        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectoryPath, sourcePath);
            CopyFileIfDifferent(sourcePath, Path.Combine(targetDirectoryPath, relativePath));
        }
    }

    private static void CopyFileIfDifferent(string sourcePath, string targetPath)
    {
        if (PathEquals(sourcePath, targetPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(sourcePath, targetPath, overwrite: true);
    }

    private static bool PathEquals(string leftPath, string rightPath)
    {
        return string.Equals(Path.GetFullPath(leftPath), Path.GetFullPath(rightPath), StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteNextCommands(string installRoot, string installedExecutablePath, bool setupComplete)
    {
        Console.WriteLine();
        Console.WriteLine("Next commands:");
        Console.WriteLine($"Set-Location {installRoot}");
        if (!setupComplete)
        {
            Console.WriteLine($".\\{Path.GetFileName(installedExecutablePath)} setup --source .");
        }

        Console.WriteLine($".\\{Path.GetFileName(installedExecutablePath)} status");
        Console.WriteLine();
    }
}
