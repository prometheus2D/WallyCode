namespace WallyCode.ConsoleApp.Copilot;

internal sealed class ProviderRegistry
{
    private readonly Dictionary<string, ILlmProvider> _providers;
    private readonly Dictionary<string, IProviderInstaller> _installers;

    public ProviderRegistry(IEnumerable<ILlmProvider> providers, IEnumerable<IProviderInstaller> installers)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _installers = installers.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ILlmProvider> All => _providers.Values.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public static ProviderRegistry Create(Runtime.AppLogger logger)
    {
        return new ProviderRegistry(
            providers:
            [
                new GhCopilotCliProvider(
                    name: "gh-copilot-claude",
                    defaultModel: "claude-sonnet-4",
                    description: "GitHub Copilot CLI preset using Claude Sonnet 4.",
                    installerKey: "gh-copilot",
                    logger: logger),
                new GhCopilotCliProvider(
                    name: "gh-copilot-gpt5",
                    defaultModel: "gpt-5",
                    description: "GitHub Copilot CLI preset using GPT-5.",
                    installerKey: "gh-copilot",
                    logger: logger)
            ],
            installers:
            [
                new GhCopilotInstaller(logger)
            ]);
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

    public IProviderInstaller? GetInstaller(string key)
    {
        return _installers.TryGetValue(key, out var installer) ? installer : null;
    }

    public IReadOnlyList<string> InstallerKeys =>
        _providers.Values
            .Select(p => p.InstallerKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
}