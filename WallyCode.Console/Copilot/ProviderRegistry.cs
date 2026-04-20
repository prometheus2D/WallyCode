namespace WallyCode.ConsoleApp.Copilot;

internal sealed class ProviderRegistry
{
    public const string DefaultProviderName = "gh-copilot-claude";

    private readonly Dictionary<string, ILlmProvider> _providers;

    public ProviderRegistry(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ILlmProvider> All => _providers.Values.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public ILlmProvider Default => Get(DefaultProviderName);

    public static ProviderRegistry Create(Runtime.AppLogger logger)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var definitions = ProviderDefinition.LoadAll(baseDirectory);
        var providers = definitions.Select(definition => CreateProvider(definition, logger)).ToList();
        return new ProviderRegistry(providers);
    }

    private static ILlmProvider CreateProvider(ProviderDefinition definition, Runtime.AppLogger logger)
    {
        return definition.Kind switch
        {
            "gh-copilot-cli" => new GhCopilotCliProvider(
                name: definition.Name,
                defaultModel: definition.DefaultModel,
                description: definition.Description,
                supportedModels: definition.SupportedModels,
                logger: logger),
            _ => throw new InvalidOperationException(
                $"Unknown provider kind '{definition.Kind}' in provider '{definition.Name}'.")
        };
    }

    public ILlmProvider Get(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("A provider name is required.");
        }

        if (_providers.TryGetValue(providerName.Trim(), out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Unknown provider '{providerName}'. Available providers: {string.Join(", ", All.Select(p => p.Name))}");
    }
}
