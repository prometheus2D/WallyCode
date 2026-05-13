using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class LoggingCommandHandler
{
    private readonly AppLogger _logger;

    public LoggingCommandHandler(AppLogger logger)
    {
        _logger = logger;
    }

    public Task<int> ExecuteAsync(LoggingCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (options.Enable && options.Disable)
        {
            throw new InvalidOperationException("Choose either --enable or --disable, not both.");
        }

        if (options.Verbose && options.Quiet)
        {
            throw new InvalidOperationException("Choose either --verbose or --quiet, not both.");
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);

        if (options.Enable)
        {
            settings.Logging.Enabled = true;
        }

        if (options.Disable)
        {
            settings.Logging.Enabled = false;
        }

        if (options.Verbose)
        {
            settings.Logging.Verbose = true;
        }

        if (options.Quiet)
        {
            settings.Logging.Verbose = false;
        }

        settings.Save(projectRoot);

        _logger.Section("WallyCode Logging");
        _logger.Info($"Project: {projectRoot}");
        _logger.Info($"Logging enabled: {settings.Logging.Enabled}");
        _logger.Info($"Verbose logging: {settings.Logging.Verbose}");
        _logger.Success("Workspace logging settings saved.");
        return Task.FromResult(0);
    }
}
