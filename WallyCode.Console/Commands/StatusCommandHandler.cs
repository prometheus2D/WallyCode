using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class StatusCommandHandler
{
    private readonly AppLogger _logger;

    public StatusCommandHandler(AppLogger logger)
    {
        _logger = logger;
    }

    public Task<int> ExecuteAsync(StatusCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, options.MemoryRoot);
        var settings = ProjectSettings.Load(projectRoot);

        _logger.Section("WallyCode Status");
        _logger.Info($"Source:       {projectRoot}");
        _logger.Info($"Memory root:  {sessionRoot}");
        _logger.Info($"Provider:     {settings.Provider}");
        _logger.Info($"Model:        {settings.Model ?? "(provider default)"}");

        if (Session.Exists(sessionRoot))
        {
            try
            {
                var session = Session.Load(sessionRoot);
                _logger.Info($"Session:      [{session.Status}] {session.WorkflowName} → {session.ActiveStepName} (iteration {session.IterationCount})");
                _logger.Info($"Goal:         {session.Goal}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Session file exists but could not be read: {ex.Message}");
            }
        }
        else
        {
            _logger.Info("Session:      (none)");
        }

        return Task.FromResult(0);
    }
}
