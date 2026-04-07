using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class SetProviderCommandHandler
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public SetProviderCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public Task<int> ExecuteAsync(SetProviderCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
            var settings = ProjectSettings.Load(projectRoot);
            var provider = _providerRegistry.Get(commandOptions.Provider);

            settings.Provider = provider.Name;
            settings.Save(projectRoot);

            _logger.Section("Project Provider");
            _logger.Info($"Project root: {projectRoot}");
            _logger.Info($"Settings file: {ProjectSettings.GetFilePath(projectRoot)}");
            _logger.Success($"Default provider set to {provider.Name} with model {provider.DefaultModel}.");

            return Task.FromResult(0);
        }
        catch (Exception exception)
        {
            _logger.Error(exception.ToString());
            return Task.FromResult(1);
        }
    }
}