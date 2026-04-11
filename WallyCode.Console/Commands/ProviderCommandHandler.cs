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

        if (!string.IsNullOrWhiteSpace(options.Model))
        {
            return SetModelAsync(options, cancellationToken);
        }

        if (options.Models)
        {
            return ListModelsAsync(options, cancellationToken);
        }

        return ListAsync(options, cancellationToken);
    }

    private int SetProvider(ProviderCommandOptions options)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        _logger.Section("WallyCode Provider");
        _logger.Info($"Initialized source: {projectRoot}");

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            _logger.Error("Provider name required. Usage: provider <name> --set");
            return 1;
        }

        var settings = ProjectSettings.Load(projectRoot);
        var provider = _registry.Get(options.Name);

        settings.Provider = provider.Name;
        settings.Model = provider.DefaultModel;
        settings.Save(projectRoot);

        _logger.Success($"Default provider set to {provider.Name} (model: {provider.DefaultModel})");
        return 0;
    }

    private async Task<int> SetModelAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        _logger.Section("WallyCode Provider");
        _logger.Info($"Initialized source: {projectRoot}");

        var settings = ProjectSettings.Load(projectRoot);
        var providerName = string.IsNullOrWhiteSpace(options.Name)
            ? settings.Provider
            : options.Name.Trim();
        var provider = _registry.Get(providerName);
        var requestedModel = options.Model!.Trim();
        var availableModels = await provider.GetAvailableModelsAsync(cancellationToken);

        if (!availableModels.Contains(requestedModel, StringComparer.OrdinalIgnoreCase))
        {
            _logger.Error($"Unknown model '{requestedModel}' for provider '{provider.Name}'. Use 'provider {provider.Name} --models' to list available models.");
            return 1;
        }

        settings.Provider = provider.Name;
        settings.Model = availableModels.First(model => string.Equals(model, requestedModel, StringComparison.OrdinalIgnoreCase));
        settings.Save(projectRoot);

        _logger.Success($"Default model for {provider.Name} set to {settings.Model}");
        return 0;
    }

    private async Task<int> ListModelsAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        _logger.Section("WallyCode Provider");
        _logger.Info($"Initialized source: {projectRoot}");

        var settings = ProjectSettings.Load(projectRoot);
        var providerName = string.IsNullOrWhiteSpace(options.Name)
            ? settings.Provider
            : options.Name.Trim();
        var provider = _registry.Get(providerName);
        var models = await provider.GetAvailableModelsAsync(cancellationToken);
        var defaultModel = string.Equals(provider.Name, settings.Provider, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(settings.Model)
            ? settings.Model
            : provider.DefaultModel;

        Console.WriteLine(provider.Name);

        foreach (var model in models)
        {
            var isDefault = string.Equals(model, defaultModel, StringComparison.OrdinalIgnoreCase) ? " (default)" : "";
            Console.WriteLine($"  {model}{isDefault}");
        }

        return 0;
    }

    private async Task<int> ListAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        _logger.Section("WallyCode Provider");
        _logger.Info($"Initialized source: {projectRoot}");

        var settings = ProjectSettings.Load(projectRoot);
        var current = _registry.Get(settings.Provider);

        foreach (var provider in _registry.All)
        {
            var readinessError = await provider.GetReadinessErrorAsync(cancellationToken);
            var status = string.IsNullOrWhiteSpace(readinessError) ? "ready" : "unavailable";
            var isDefault = string.Equals(provider.Name, current.Name, StringComparison.OrdinalIgnoreCase) ? " (default)" : "";
            var defaultModel = string.Equals(provider.Name, current.Name, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(settings.Model)
                ? settings.Model
                : provider.DefaultModel;

            Console.WriteLine($"  {provider.Name} [{status}] model:{defaultModel}{isDefault}");
        }

        return 0;
    }
}
