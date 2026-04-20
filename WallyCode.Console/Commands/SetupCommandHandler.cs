using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class SetupCommandHandler
{
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

        if (options.Force)
        {
            ResetSetup(targetDirectory);
            _logger.Success("Fresh setup complete.");
        }
        else
        {
            var createdAny = EnsureSetup(targetDirectory);

            if (createdAny)
            {
                _logger.Success("Setup complete.");
            }
            else
            {
                _logger.Info("Setup already in place.");
            }
        }

        WriteNextCommands(targetDirectory);
        return Task.FromResult(0);
    }

    private string ResolveTargetDirectory(SetupCommandOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
        {
            try
            {
                return Path.GetFullPath(options.DirectoryPath.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException(
                    $"Invalid setup target '{options.DirectoryPath}'. {exception.Message}",
                    exception);
            }
        }

        if (options.VsBuild)
        {
            return ResolveVsBuildTargetDirectory();
        }

        return _appDirectoryPath;
    }

    private bool EnsureSetup(string targetDirectory)
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

        return createdAny;
    }

    private void ResetSetup(string targetDirectory)
    {
        var settingsPath = ProjectSettings.GetFilePath(targetDirectory);
        var runtimeDirectoryPath = GetRuntimeDirectoryPath(targetDirectory);

        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }

        if (Directory.Exists(runtimeDirectoryPath))
        {
            Directory.Delete(runtimeDirectoryPath, recursive: true);
        }

        WriteDefaultSettings(targetDirectory);
        Directory.CreateDirectory(runtimeDirectoryPath);
    }

    private void WriteDefaultSettings(string targetDirectory)
    {
        var provider = _providerRegistry.Default;
        var settings = new ProjectSettings
        {
            Provider = provider.Name,
            Model = provider.DefaultModel
        };

        settings.Save(targetDirectory);
    }

    private string ResolveVsBuildTargetDirectory()
    {
        var buildProjectDirectory = FindBuildProjectDirectory(_appDirectoryPath)
            ?? throw new InvalidOperationException(
                $"--vs-build requires the app directory to be under bin{Path.DirectorySeparatorChar}Debug or bin{Path.DirectorySeparatorChar}Release. Current app directory: {_appDirectoryPath}");

        var workspaceRoot = FindTopmostWorkspaceMarker(buildProjectDirectory)
            ?? throw new InvalidOperationException(
                $"Could not find a workspace root above {buildProjectDirectory}. Looked for .git, *.sln, or wallycode.json.");

        return workspaceRoot;
    }

    private static string? FindBuildProjectDirectory(string appDirectoryPath)
    {
        var trimmedPath = appDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        for (var current = new DirectoryInfo(trimmedPath); current is not null; current = current.Parent)
        {
            if (current.Parent is null || current.Parent.Parent is null)
            {
                continue;
            }

            var isConfigurationDirectory = string.Equals(current.Name, "Debug", StringComparison.OrdinalIgnoreCase)
                || string.Equals(current.Name, "Release", StringComparison.OrdinalIgnoreCase);

            if (isConfigurationDirectory && string.Equals(current.Parent.Name, "bin", StringComparison.OrdinalIgnoreCase))
            {
                return current.Parent.Parent.FullName;
            }
        }

        return null;
    }

    private static string? FindTopmostWorkspaceMarker(string startDirectory)
    {
        string? candidate = null;

        for (var current = new DirectoryInfo(startDirectory); current is not null; current = current.Parent)
        {
            if (ContainsWorkspaceMarker(current.FullName))
            {
                candidate = current.FullName;
            }
        }

        return candidate;
    }

    private static bool ContainsWorkspaceMarker(string directoryPath)
    {
        return Directory.Exists(Path.Combine(directoryPath, ".git"))
            || File.Exists(Path.Combine(directoryPath, "wallycode.json"))
            || Directory.EnumerateFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly).Any();
    }

    private static string GetRuntimeDirectoryPath(string targetDirectory)
    {
        return Path.Combine(targetDirectory, ".wallycode");
    }

    private static void WriteNextCommands(string targetDirectory)
    {
        Console.WriteLine();
        Console.WriteLine("Next commands:");
        Console.WriteLine($"cd {targetDirectory}");
        Console.WriteLine("wallycode provider");
        Console.WriteLine("wallycode loop --definition ask \"Summarize this repository in one short paragraph.\"");
        Console.WriteLine();
    }
}