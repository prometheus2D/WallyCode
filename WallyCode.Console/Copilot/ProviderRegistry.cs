namespace WallyCode.ConsoleApp.Copilot;

internal sealed class ProviderRegistry
{
    private readonly Dictionary<string, ILlmProvider> _providers;

    public ProviderRegistry(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ILlmProvider> All => _providers.Values.OrderBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public static ProviderRegistry Create(Runtime.AppLogger logger)
    {
        return new ProviderRegistry(
        new[]
        {
            new GhCopilotCliProvider(
                name: "gh-copilot-claude",
                defaultModel: "claude-sonnet-4",
                description: "GitHub Copilot CLI preset using Claude Sonnet 4.",
                logger: logger),
            new GhCopilotCliProvider(
                name: "gh-copilot-gpt5",
                defaultModel: "gpt-5",
                description: "GitHub Copilot CLI preset using GPT-5.",
                logger: logger)
        });
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
            $"Unknown provider '{providerName}'. Available providers: {string.Join(", ", All.Select(provider => provider.Name))}");
    }
}