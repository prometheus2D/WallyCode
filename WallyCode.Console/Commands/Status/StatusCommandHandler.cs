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

        var (projectRoot, settings) = ProjectSettings.ResolveInitializedProjectContext(options.SourcePath);
        var sessionRoot = ProjectSettings.ResolveSessionRoot(settings, projectRoot, options.MemoryRoot);
        var activeProjectPath = ProjectSettings.ResolveActiveProjectPath();
        var activeProjectFilePath = ProjectSettings.GetActiveProjectFilePath();

        _logger.Section("WallyCode Status");
        _logger.Info($"Source:       {projectRoot}");
        _logger.Info($"Active file:  {activeProjectFilePath}");
        _logger.Info($"Active source: {activeProjectPath ?? "(none)"}");
        _logger.Info($"Memory root:  {sessionRoot}");
        _logger.Info($"Provider:     {settings.Provider}");
        _logger.Info($"Model:        {settings.Model ?? "(provider default)"}");
        _logger.Info($"Default memory root:{settings.RuntimeDefaults.MemoryRoot ?? "(none)"}");
        _logger.Info($"Default max-run:    {settings.RuntimeDefaults.MaxRunIterations?.ToString() ?? "(none)"}");
        _logger.Info($"Default max-total:  {settings.RuntimeDefaults.MaxTotalIterations?.ToString() ?? "(none)"}");
        _logger.Info($"Default max-repeat: {settings.RuntimeDefaults.MaxStepRepeats?.ToString() ?? "(none)"}");

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
