using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class SetupCommandHandler
{
    private const string DeployedExecutableName = "wallycode.exe";

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;
    private readonly string _appDirectoryPath;

    public SetupCommandHandler(ProviderRegistry providerRegistry, AppLogger logger, string? appDirectoryPath = null)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
        _appDirectoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppContext.BaseDirectory
            : appDirectoryPath);
    }

    public Task<int> ExecuteAsync(SetupCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targetDirectory = ResolveTargetDirectory(options);
        try
        {
            Directory.CreateDirectory(targetDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"Invalid setup target '{targetDirectory}'. {exception.Message}",
                exception);
        }

        _logger.Section("WallyCode Setup");
        _logger.Info($"Setup target: {targetDirectory}");

        if (options.Cleanup)
        {
            var cleanupHandler = new CleanupCommandHandler(_logger, _appDirectoryPath);
            cleanupHandler.ExecuteAsync(
                new CleanupCommandOptions { SourcePath = targetDirectory, PreserveDeployedPayload = true },
                cancellationToken).GetAwaiter().GetResult();
        }

        var activeProjectDirectory = options.Deploy ? null : _appDirectoryPath;
        var createdAny = EnsureSetup(targetDirectory, activeProjectDirectory);

        // Enforce setup requirements for commands
        if (options.RequiresSetup && !Directory.Exists(targetDirectory))
        {
            throw new InvalidOperationException("Setup environment is required but not found.");
        }

        if (createdAny)
        {
            _logger.Success("Setup complete.");
        }
        else
        {
            _logger.Info("Setup already in place.");
        }

        string? deployedExecutablePath = null;
        if (options.Deploy)
        {
            deployedExecutablePath = DeployExecutable(targetDirectory);
            _logger.Success($"Deployment successful: {deployedExecutablePath}");
            _logger.Info($"Run the deployed executable from its new location: {deployedExecutablePath}");
        }

        WriteNextCommands(targetDirectory, deployedExecutablePath);
        return Task.FromResult(0);
    }

    private string ResolveTargetDirectory(SetupCommandOptions options)
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
                    $"Invalid setup target '{options.SourcePath}'. {exception.Message}",
                    exception);
            }
        }

        if (options.VsBuild)
        {
            return WorkspacePathResolver.ResolveVsBuildWorkspaceRoot(_appDirectoryPath);
        }

        return _appDirectoryPath;
    }

    private bool EnsureSetup(string targetDirectory, string? activeProjectDirectory)
    {
        var createdAny = false;
        var settingsPath = ProjectSettings.GetFilePath(targetDirectory);
        var runtimeDirectoryPath = GetRuntimeDirectoryPath(targetDirectory);

        if (!File.Exists(settingsPath))
        {
            WriteDefaultSettings(targetDirectory);
            _logger.Success("Created wallycode.json.");
            createdAny = true;
        }

        if (!Directory.Exists(runtimeDirectoryPath))
        {
            Directory.CreateDirectory(runtimeDirectoryPath);
            _logger.Success("Created .wallycode.");
            createdAny = true;
        }

        ProjectSettings.Load(targetDirectory).Save(targetDirectory);
        if (!string.IsNullOrWhiteSpace(activeProjectDirectory))
        {
            ProjectSettings.SaveActiveProjectPath(targetDirectory, activeProjectDirectory);
            _logger.Success("Updated wallycode.active.json.");
            createdAny = true;
        }

        return createdAny;
    }

    private void WriteDefaultSettings(string targetDirectory)
    {
        var provider = _providerRegistry.Default;
        var settings = new ProjectSettings
        {
            Provider = provider.Name,
            Model = provider.DefaultModel,
            RuntimeDefaults = new RuntimeDefaultsSettings()
        };

        settings.Save(targetDirectory);
    }

    private static string GetRuntimeDirectoryPath(string targetDirectory)
    {
        return Path.Combine(targetDirectory, ".wallycode");
    }

    private string DeployExecutable(string targetDirectory)
    {
        var sourceExecutablePath = ResolveSourceExecutablePath();
        var deployedExecutablePath = Path.Combine(targetDirectory, DeployedExecutableName);

        CopyFileIfDifferent(sourceExecutablePath, deployedExecutablePath);
        var runtimeCompanionPaths = CopyRuntimeCompanionFiles(sourceExecutablePath, targetDirectory);
        var loadablesPath = CopyLoadables(targetDirectory);
        ProjectSettings.SaveActiveProjectPath(targetDirectory, targetDirectory);
        DeploymentManifest.Save(
            targetDirectory,
            [deployedExecutablePath, .. runtimeCompanionPaths, ProjectSettings.GetActiveProjectFilePath(targetDirectory)],
            loadablesPath is null ? [] : [loadablesPath]);
        _logger.Success("Updated deployed wallycode.active.json.");

        return deployedExecutablePath;
    }

    private string ResolveSourceExecutablePath()
    {
        var preferredExecutablePath = Path.Combine(_appDirectoryPath, DeployedExecutableName);
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

    private IReadOnlyList<string> CopyRuntimeCompanionFiles(string sourceExecutablePath, string targetDirectory)
    {
        var copiedPaths = new List<string>();
        foreach (var sourcePath in Directory.EnumerateFiles(_appDirectoryPath, "*", SearchOption.TopDirectoryOnly))
        {
            if (PathEquals(sourcePath, sourceExecutablePath) || !IsRuntimeCompanionFile(sourcePath))
            {
                continue;
            }

            var targetPath = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));
            CopyFileIfDifferent(sourcePath, targetPath);
            copiedPaths.Add(targetPath);
        }

        return copiedPaths;
    }

    private string? CopyLoadables(string targetDirectory)
    {
        var sourceLoadablesPath = Path.Combine(_appDirectoryPath, "Loadables");
        if (!Directory.Exists(sourceLoadablesPath))
        {
            return null;
        }

        var targetLoadablesPath = Path.Combine(targetDirectory, "Loadables");
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

    private static void WriteNextCommands(string targetDirectory, string? deployedExecutablePath)
    {
        Console.WriteLine();
        Console.WriteLine("Next commands:");
        Console.WriteLine($"Active source: {targetDirectory}");
        if (!string.IsNullOrWhiteSpace(deployedExecutablePath))
        {
            Console.WriteLine($"Set-Location {targetDirectory}");
            Console.WriteLine($".\\{Path.GetFileName(deployedExecutablePath)} provider");
            Console.WriteLine($".\\{Path.GetFileName(deployedExecutablePath)} run \"Summarize this repository in one short paragraph.\" ask");
        }
        else
        {
            Console.WriteLine("wallycode provider");
            Console.WriteLine("wallycode run \"Summarize this repository in one short paragraph.\" ask");
        }
        Console.WriteLine();
    }
}