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
            var providers = _providerRegistry.All;
            var readiness = new List<(ILlmProvider Provider, string Status)>();

            foreach (var provider in providers)
            {
                var readinessError = await provider.GetReadinessErrorAsync(cancellationToken);
                readiness.Add((provider, string.IsNullOrWhiteSpace(readinessError) ? "ready" : "unavailable"));
            }

            _logger.Section("Providers");
            Console.WriteLine($"Current : {currentProvider.Name}");
            Console.WriteLine($"Model   : {currentProvider.DefaultModel}");
            Console.WriteLine();

            foreach (var item in readiness)
            {
                Console.WriteLine($"- {item.Provider.Name} [{item.Status}]");
                Console.WriteLine($"  Model: {item.Provider.DefaultModel}");
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