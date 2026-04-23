using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.Tests.Copilot;

public class ProviderRegistryTests
{
    [Fact]
    public void Registry_can_be_constructed_from_json_backed_provider_definitions()
    {
        var definitions = new[]
        {
            new ProviderDefinition
            {
                Name = "provider-a",
                Kind = "gh-copilot-cli",
                Description = "Provider A",
                DefaultModel = "model-a",
                SupportedModels = ["model-a", "model-b"]
            },
            new ProviderDefinition
            {
                Name = "provider-b",
                Kind = "gh-copilot-cli",
                Description = "Provider B",
                DefaultModel = "model-c",
                SupportedModels = ["model-c"]
            }
        };

        var registry = new ProviderRegistry(definitions.Select(definition =>
            new GhCopilotCliProvider(
                definition.Name,
                definition.DefaultModel,
                definition.Description,
                definition.SupportedModels,
                new AppLogger())));

        Assert.Equal(2, registry.All.Count);
        Assert.Equal("provider-a", registry.Get("provider-a").Name);
        Assert.Equal(["model-a", "model-b"], registry.Get("provider-a").SupportedModels);
    }
}
