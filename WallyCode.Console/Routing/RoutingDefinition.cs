using System.Text.Json;

namespace WallyCode.ConsoleApp.Routing;

internal sealed class LogicalUnit
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> AllowedKeywords { get; set; } = [];
    public Dictionary<string, string> Transitions { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class RoutingDefinition
{
    public string Name { get; set; } = string.Empty;
    public string StartUnitName { get; set; } = string.Empty;
    public List<LogicalUnit> Units { get; set; } = [];

    public LogicalUnit GetUnit(string name) =>
        Units.FirstOrDefault(u => u.Name == name)
        ?? throw new InvalidOperationException($"Routing definition '{Name}' has no unit '{name}'.");

    public static RoutingDefinition LoadByName(string definitionName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Routing", "Definitions", $"{definitionName}.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Routing definition '{definitionName}' not found at {path}.");
        }
        return LoadFromJson(File.ReadAllText(path));
    }

    public static RoutingDefinition LoadFromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<RoutingDefinition>(json, JsonOptions.Default)
            ?? throw new InvalidOperationException("Routing definition JSON is empty or invalid.");
        definition.Validate();
        return definition;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Routing definition name is required.");
        }
        if (Units.Count == 0)
        {
            throw new InvalidOperationException($"Routing definition '{Name}' must declare at least one unit.");
        }

        var unitNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in Units)
        {
            if (string.IsNullOrWhiteSpace(unit.Name))
            {
                throw new InvalidOperationException($"Routing definition '{Name}' contains a unit with no name.");
            }
            if (!unitNames.Add(unit.Name))
            {
                throw new InvalidOperationException($"Routing definition '{Name}' has duplicate unit '{unit.Name}'.");
            }
            foreach (var transitionKey in unit.Transitions.Keys)
            {
                if (!unit.AllowedKeywords.Contains(transitionKey))
                {
                    throw new InvalidOperationException($"Unit '{unit.Name}' transition '{transitionKey}' is not in allowedKeywords.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(StartUnitName) || !unitNames.Contains(StartUnitName))
        {
            throw new InvalidOperationException($"Routing definition '{Name}' startUnitName '{StartUnitName}' is not a declared unit.");
        }

        foreach (var unit in Units)
        {
            foreach (var (_, target) in unit.Transitions)
            {
                if (!unitNames.Contains(target))
                {
                    throw new InvalidOperationException($"Unit '{unit.Name}' transition targets unknown unit '{target}'.");
                }
            }
        }
    }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
