using WallyCode.ConsoleApp.Copilot;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Copilot;

public class ProviderDefinitionTests
{
    [Fact]
    public void LoadFromFile_normalizes_and_includes_default_model()
    {
        using var workspace = TempWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "provider.json");
        File.WriteAllText(filePath, """
        {
          "name": " sample-provider ",
          "kind": " gh-copilot-cli ",
          "description": " Sample provider ",
          "defaultModel": " model-a ",
          "supportedModels": ["model-b", "model-a", "model-b"]
        }
        """);

        var definition = ProviderDefinition.LoadFromFile(filePath);

        Assert.Equal("sample-provider", definition.Name);
        Assert.Equal("gh-copilot-cli", definition.Kind);
        Assert.Equal("Sample provider", definition.Description);
        Assert.Equal("model-a", definition.DefaultModel);
        Assert.Equal(["model-b", "model-a"], definition.SupportedModels);
    }

    [Fact]
    public void LoadAll_loads_provider_definitions_from_providers_directory()
    {
        using var workspace = TempWorkspace.Create();
        var providersDirectory = Path.Combine(workspace.RootPath, "Providers");
        Directory.CreateDirectory(providersDirectory);

        File.WriteAllText(Path.Combine(providersDirectory, "a.json"), """
        {
          "name": "provider-a",
          "kind": "gh-copilot-cli",
          "description": "Provider A",
          "defaultModel": "model-a"
        }
        """);

        File.WriteAllText(Path.Combine(providersDirectory, "b.json"), """
        {
          "name": "provider-b",
          "kind": "gh-copilot-cli",
          "description": "Provider B",
          "defaultModel": "model-b"
        }
        """);

        var definitions = ProviderDefinition.LoadAll(workspace.RootPath);

        Assert.Equal(2, definitions.Count);
        Assert.Equal(["provider-a", "provider-b"], definitions.Select(d => d.Name).ToArray());
    }
}
