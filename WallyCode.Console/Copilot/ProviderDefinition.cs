using System.Text.Json;
using System.Text.Json.Serialization;

namespace WallyCode.ConsoleApp.Copilot;

internal sealed class ProviderDefinition
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string Description { get; init; }
    public required string DefaultModel { get; init; }
    public string? PreferredCheapModel { get; init; }
    public List<string> SupportedModels { get; init; } = [];

    public static IReadOnlyList<ProviderDefinition> LoadAll(string baseDirectory)
    {
        var providersDirectory = Path.Combine(baseDirectory, "Providers");

        if (!Directory.Exists(providersDirectory))
        {
            throw new DirectoryNotFoundException($"Provider definitions directory not found: {providersDirectory}");
        }

        var definitions = Directory
            .EnumerateFiles(providersDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LoadFromFile)
            .OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (definitions.Count == 0)
        {
            throw new InvalidOperationException($"No provider definitions found in {providersDirectory}.");
        }

        var duplicateNames = definitions
            .GroupBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate provider definitions found: {string.Join(", ", duplicateNames)}");
        }

        return definitions;
    }

    public static ProviderDefinition LoadFromFile(string filePath)
    {
        var definition = JsonSerializer.Deserialize<ProviderDefinition>(File.ReadAllText(filePath), SerializerOptions)
            ?? throw new InvalidOperationException($"Provider definition file '{filePath}' is empty or invalid.");

        return Normalize(definition, filePath);
    }

    private static ProviderDefinition Normalize(ProviderDefinition definition, string filePath)
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidOperationException($"Provider definition '{filePath}' is missing 'name'.");
        }

        if (string.IsNullOrWhiteSpace(definition.Kind))
        {
            throw new InvalidOperationException($"Provider definition '{filePath}' is missing 'kind'.");
        }

        if (string.IsNullOrWhiteSpace(definition.Description))
        {
            throw new InvalidOperationException($"Provider definition '{filePath}' is missing 'description'.");
        }

        if (string.IsNullOrWhiteSpace(definition.DefaultModel))
        {
            throw new InvalidOperationException($"Provider definition '{filePath}' is missing 'defaultModel'.");
        }

        var normalizedDefaultModel = definition.DefaultModel.Trim();
        var supportedModels = definition.SupportedModels
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!supportedModels.Contains(normalizedDefaultModel, StringComparer.OrdinalIgnoreCase))
        {
            supportedModels.Insert(0, normalizedDefaultModel);
        }

        return new ProviderDefinition
        {
            Name = definition.Name.Trim(),
            Kind = definition.Kind.Trim(),
            Description = definition.Description.Trim(),
            DefaultModel = normalizedDefaultModel,
            PreferredCheapModel = string.IsNullOrWhiteSpace(definition.PreferredCheapModel) ? null : definition.PreferredCheapModel.Trim(),
            SupportedModels = supportedModels
        };
    }
}
