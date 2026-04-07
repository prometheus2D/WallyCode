using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ProvidersCommandHandler
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public ProvidersCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(ProvidersCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
            var settings = ProjectSettings.Load(projectRoot);
            var currentProvider = _providerRegistry.Get(settings.Provider);

            _logger.Section("Providers");
            _logger.Info($"Project root: {projectRoot}");
            _logger.Info($"Settings file: {ProjectSettings.GetFilePath(projectRoot)}");
            _logger.Info($"Current project provider: {settings.Provider}");
            _logger.Info($"Current project model: {currentProvider.DefaultModel}");

            Console.WriteLine();

            foreach (var provider in _providerRegistry.All)
            {
                var readinessError = await provider.GetReadinessErrorAsync(cancellationToken);

                Console.WriteLine($"- {provider.Name}");
                Console.WriteLine($"  Default model: {provider.DefaultModel}");
                Console.WriteLine($"  Status: {(string.IsNullOrWhiteSpace(readinessError) ? "ready" : "unavailable")}");

                if (!string.IsNullOrWhiteSpace(readinessError))
                {
                    Console.WriteLine($"  Reason: {readinessError}");
                }

                Console.WriteLine($"  {provider.Description}");
                Console.WriteLine("  Use --model <name> on prompt or loop for a one-off override.");
            }

            return 0;
        }
        catch (Exception exception)
        {
            _logger.Error(exception.ToString());
            return 1;
        }
    }
}