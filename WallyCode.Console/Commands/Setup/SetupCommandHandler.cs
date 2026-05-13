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

        if (options.Cleanup)
        {
            var cleanupHandler = new CleanupCommandHandler(_logger, _appDirectoryPath);
            cleanupHandler.ExecuteAsync(
                new CleanupCommandOptions { SourcePath = targetDirectory },
                cancellationToken).GetAwaiter().GetResult();
        }

        var createdAny = EnsureSetup(targetDirectory);

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

        WriteNextCommands(targetDirectory);
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

        // Persist the resolved environment folder in wallycode.json
        var runtimeDefaults = new RuntimeDefaultsSettings { SourcePath = targetDirectory };
        // Set RuntimeDefaults and save settings
        var projectSettings = new ProjectSettings { RuntimeDefaults = runtimeDefaults };
        projectSettings.Save(targetDirectory);

        return createdAny;
    }

    private void WriteDefaultSettings(string targetDirectory)
    {
        var provider = _providerRegistry.Default;
        var settings = new ProjectSettings
        {
            Provider = provider.Name,
            Model = provider.DefaultModel,
            RuntimeDefaults = new RuntimeDefaultsSettings
            {
                SourcePath = targetDirectory
            }
        };

        settings.Save(targetDirectory);
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
        Console.WriteLine("wallycode run \"Summarize this repository in one short paragraph.\" ask");
        Console.WriteLine();
    }
}