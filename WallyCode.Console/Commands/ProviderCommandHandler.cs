using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ProviderCommandHandler
{
    private readonly ProviderRegistry _registry;
    private readonly AppLogger _logger;

    public ProviderCommandHandler(ProviderRegistry registry, AppLogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public Task<int> ExecuteAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (options.Set)
        {
            return Task.FromResult(SetProvider(options));
        }

        return ListAsync(options, cancellationToken);
    }

    private int SetProvider(ProviderCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            _logger.Error("Provider name required. Usage: provider <name> --set");
            return 1;
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var provider = _registry.Get(options.Name);

        settings.Provider = provider.Name;
        settings.Save(projectRoot);

        _logger.Success($"{provider.Name} (model: {provider.DefaultModel})");
        return 0;
    }

    private async Task<int> ListAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var current = _registry.Get(settings.Provider);

        foreach (var provider in _registry.All)
        {
            var readinessError = await provider.GetReadinessErrorAsync(cancellationToken);
            var status = string.IsNullOrWhiteSpace(readinessError) ? "ready" : "unavailable";
            var active = string.Equals(provider.Name, current.Name, StringComparison.OrdinalIgnoreCase) ? " *" : "";

            Console.WriteLine($"  {provider.Name} [{status}] model:{provider.DefaultModel}{active}");
        }

        return 0;
    }
}
