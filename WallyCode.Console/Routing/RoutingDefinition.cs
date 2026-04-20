using System.Text.Json;

namespace WallyCode.ConsoleApp.Routing;

internal static class UnitExecutionKind
{
    public const string Provider = "provider";
    public const string Script = "script";
}

internal sealed class KeywordDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("Keyword definition id is required.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new InvalidOperationException($"Keyword definition '{Id}' must have a description.");
        }
    }
}

internal sealed class KeywordOption
{
    public string Keyword { get; set; } = string.Empty;
    public string? Description { get; set; }

    public void Validate(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(Keyword))
        {
            throw new InvalidOperationException($"Logical unit '{ownerName}' contains a keyword option with no keyword.");
        }
    }
}

internal class LogicalUnit
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> AllowedKeywords { get; set; } = [];
    public List<KeywordOption> KeywordOptions { get; set; } = [];
    public Dictionary<string, string> Transitions { get; set; } = new(StringComparer.Ordinal);
    public string ExecutionKind { get; set; } = UnitExecutionKind.Provider;
    public string? ScriptPath { get; set; }

    public string QualifiedName(string definitionName) => $"{definitionName}/{Name}";

    public string DescribeKeyword(string keyword)
    {
        var option = KeywordOptions.FirstOrDefault(x => string.Equals(x.Keyword, keyword, StringComparison.Ordinal));
        return option?.Description ?? string.Empty;
    }

    public void ValidateShape(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Routing definition '{ownerName}' contains a unit with no name.");
        }

        if (string.IsNullOrWhiteSpace(ExecutionKind))
        {
            ExecutionKind = UnitExecutionKind.Provider;
        }

        if (!string.Equals(ExecutionKind, UnitExecutionKind.Provider, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(ExecutionKind, UnitExecutionKind.Script, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unit '{ownerName}/{Name}' has unsupported executionKind '{ExecutionKind}'.");
        }

        if (string.Equals(ExecutionKind, UnitExecutionKind.Script, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(ScriptPath))
        {
            throw new InvalidOperationException($"Unit '{ownerName}/{Name}' uses executionKind 'script' but has no scriptPath.");
        }

        var describedKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in KeywordOptions)
        {
            option.Validate($"{ownerName}/{Name}");
            if (!describedKeywords.Add(option.Keyword))
            {
                throw new InvalidOperationException($"Unit '{ownerName}/{Name}' has duplicate keyword option '{option.Keyword}'.");
            }
            if (string.IsNullOrWhiteSpace(option.Description))
            {
                throw new InvalidOperationException($"Unit '{ownerName}/{Name}' keyword '{option.Keyword}' must have a description.");
            }
        }

        foreach (var keyword in AllowedKeywords)
        {
            if (!describedKeywords.Contains(keyword))
            {
                throw new InvalidOperationException($"Unit '{ownerName}/{Name}' allowed keyword '{keyword}' must have a keyword option description.");
            }
        }

        foreach (var transitionKey in Transitions.Keys)
        {
            if (!AllowedKeywords.Contains(transitionKey))
            {
                throw new InvalidOperationException($"Unit '{Name}' transition '{transitionKey}' is not in allowedKeywords.");
            }

            if (!describedKeywords.Contains(transitionKey))
            {
                throw new InvalidOperationException($"Unit '{ownerName}/{Name}' transition keyword '{transitionKey}' must have a keyword option description.");
            }
        }
    }
}

internal sealed class SharedLogicalUnitDefinition : LogicalUnit
{
    public string Id { get; set; } = string.Empty;
}

internal sealed class UnitReference
{
    public string Ref { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? PromptAddon { get; set; }
    public Dictionary<string, string> TransitionOverrides { get; set; } = new(StringComparer.Ordinal);
    public List<KeywordOption> KeywordOptionOverrides { get; set; } = [];
    public string? ExecutionKind { get; set; }
    public string? ScriptPath { get; set; }
}

internal sealed class RoutingDefinition
{
    public string Name { get; set; } = string.Empty;
    public string StartUnitName { get; set; } = string.Empty;
    public List<LogicalUnit> Units { get; set; } = [];
    public List<UnitReference> UnitRefs { get; set; } = [];

    public LogicalUnit GetUnit(string name) =>
        Units.FirstOrDefault(u => u.Name == name)
        ?? throw new InvalidOperationException($"Routing definition '{Name}' has no unit '{name}'.");

    public static RoutingDefinition LoadByName(string definitionName)
    {
        return RoutingCatalog.LoadFromBaseDirectory().GetDefinition(definitionName);
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
            unit.ValidateShape(Name);
            if (!unitNames.Add(unit.Name))
            {
                throw new InvalidOperationException($"Routing definition '{Name}' has duplicate unit '{unit.Name}'.");
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
                if (target.Contains('/', StringComparison.Ordinal))
                {
                    continue;
                }

                if (!unitNames.Contains(target))
                {
                    throw new InvalidOperationException($"Unit '{unit.Name}' transition targets unknown unit '{target}'.");
                }
            }
        }
    }
}

internal sealed class RoutingCatalog
{
    private readonly Dictionary<string, RoutingDefinition> _definitions;
    private readonly Dictionary<string, SharedLogicalUnitDefinition> _sharedUnits;
    private readonly Dictionary<string, KeywordDefinition> _keywords;

    private RoutingCatalog(
        Dictionary<string, RoutingDefinition> definitions,
        Dictionary<string, SharedLogicalUnitDefinition> sharedUnits,
        Dictionary<string, KeywordDefinition> keywords)
    {
        _definitions = definitions;
        _sharedUnits = sharedUnits;
        _keywords = keywords;
    }

    public static RoutingCatalog LoadFromBaseDirectory()
    {
        var routingRoot = Path.Combine(AppContext.BaseDirectory, "Routing");
        return LoadFromDirectory(routingRoot);
    }

    public static RoutingCatalog LoadFromDirectory(string routingRoot)
    {
        var definitionsPath = Path.Combine(routingRoot, "Definitions");
        var unitsPath = Path.Combine(routingRoot, "Units");
        var keywordsPath = Path.Combine(routingRoot, "Keywords");

        var keywords = Directory.Exists(keywordsPath)
            ? Directory.GetFiles(keywordsPath, "*.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<KeywordDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Keyword definition JSON is invalid: {path}"))
                .ToDictionary(k => k.Id, StringComparer.Ordinal)
            : new Dictionary<string, KeywordDefinition>(StringComparer.Ordinal);

        foreach (var keyword in keywords.Values)
        {
            keyword.Validate();
        }

        var sharedUnits = Directory.Exists(unitsPath)
            ? Directory.GetFiles(unitsPath, "*.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<SharedLogicalUnitDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Shared unit JSON is invalid: {path}"))
                .ToDictionary(u => u.Id, StringComparer.Ordinal)
            : new Dictionary<string, SharedLogicalUnitDefinition>(StringComparer.Ordinal);

        foreach (var unit in sharedUnits.Values)
        {
            if (string.IsNullOrWhiteSpace(unit.Id))
            {
                throw new InvalidOperationException("Shared logical unit id is required.");
            }

            if (string.IsNullOrWhiteSpace(unit.Name))
            {
                unit.Name = unit.Id;
            }

            ApplyKeywordDefinitions(unit, keywords, $"shared:{unit.Id}");
            unit.ValidateShape($"shared:{unit.Id}");
        }

        var definitions = Directory.Exists(definitionsPath)
            ? Directory.GetFiles(definitionsPath, "*.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<RoutingDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Routing definition JSON is invalid: {path}"))
                .ToDictionary(d => d.Name, StringComparer.Ordinal)
            : new Dictionary<string, RoutingDefinition>(StringComparer.Ordinal);

        var catalog = new RoutingCatalog(definitions, sharedUnits, keywords);
        catalog.ResolveAndValidate();
        return catalog;
    }

    public RoutingDefinition GetDefinition(string name) =>
        _definitions.TryGetValue(name, out var definition)
            ? definition
            : throw new InvalidOperationException($"Routing definition '{name}' not found.");

    private void ResolveAndValidate()
    {
        foreach (var definition in _definitions.Values)
        {
            var resolvedUnits = new List<LogicalUnit>();
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var unitRef in definition.UnitRefs)
            {
                if (string.IsNullOrWhiteSpace(unitRef.Ref))
                {
                    throw new InvalidOperationException($"Routing definition '{definition.Name}' contains an empty unit ref.");
                }

                if (!_sharedUnits.TryGetValue(unitRef.Ref, out var shared))
                {
                    throw new InvalidOperationException($"Routing definition '{definition.Name}' references unknown shared unit '{unitRef.Ref}'.");
                }

                var resolved = ResolveReferencedUnit(shared, unitRef);
                if (!names.Add(resolved.Name))
                {
                    throw new InvalidOperationException($"Routing definition '{definition.Name}' has duplicate unit '{resolved.Name}'.");
                }

                resolvedUnits.Add(resolved);
            }

            foreach (var unit in definition.Units)
            {
                ApplyKeywordDefinitions(unit, _keywords, definition.Name);
                unit.ValidateShape(definition.Name);
                if (!names.Add(unit.Name))
                {
                    throw new InvalidOperationException($"Routing definition '{definition.Name}' has duplicate unit '{unit.Name}'.");
                }

                resolvedUnits.Add(unit);
            }

            definition.Units = resolvedUnits;
            definition.Validate();
        }

        var qualifiedNames = _definitions.Values
            .SelectMany(d => d.Units.Select(u => u.QualifiedName(d.Name)))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var definition in _definitions.Values)
        {
            foreach (var unit in definition.Units)
            {
                foreach (var (_, target) in unit.Transitions)
                {
                    if (target.Contains('/', StringComparison.Ordinal) && !qualifiedNames.Contains(target))
                    {
                        throw new InvalidOperationException($"Unit '{definition.Name}/{unit.Name}' transition targets unknown unit '{target}'.");
                    }
                }
            }
        }
    }

    private static void ApplyKeywordDefinitions(LogicalUnit unit, IReadOnlyDictionary<string, KeywordDefinition> keywords, string ownerName)
    {
        var optionsByKeyword = unit.KeywordOptions.ToDictionary(x => x.Keyword, StringComparer.Ordinal);
        var resolvedOptions = new List<KeywordOption>();

        foreach (var keyword in unit.AllowedKeywords)
        {
            optionsByKeyword.TryGetValue(keyword, out var option);
            if (option is null)
            {
                option = new KeywordOption { Keyword = keyword };
            }

            if (string.IsNullOrWhiteSpace(option.Description))
            {
                if (!keywords.TryGetValue(keyword, out var keywordDefinition))
                {
                    throw new InvalidOperationException($"Unit '{ownerName}/{unit.Name}' keyword '{keyword}' has no description and no shared keyword definition.");
                }

                option.Description = keywordDefinition.Description;
            }

            resolvedOptions.Add(new KeywordOption { Keyword = keyword, Description = option.Description });
        }

        unit.KeywordOptions = resolvedOptions;
    }

    private static LogicalUnit ResolveReferencedUnit(SharedLogicalUnitDefinition shared, UnitReference unitRef)
    {
        var resolved = CloneSharedUnit(shared);

        if (!string.IsNullOrWhiteSpace(unitRef.Name))
        {
            resolved.Name = unitRef.Name;
        }

        if (!string.IsNullOrWhiteSpace(unitRef.PromptAddon))
        {
            resolved.Instructions = string.IsNullOrWhiteSpace(resolved.Instructions)
                ? unitRef.PromptAddon.Trim()
                : $"{resolved.Instructions.Trim()}\n\n{unitRef.PromptAddon.Trim()}";
        }

        foreach (var option in unitRef.KeywordOptionOverrides)
        {
            option.Validate($"shared:{shared.Id}");
            var existing = resolved.KeywordOptions.FirstOrDefault(x => string.Equals(x.Keyword, option.Keyword, StringComparison.Ordinal));
            if (existing is null)
            {
                throw new InvalidOperationException($"Routing definition reference '{shared.Id}' cannot override unknown keyword '{option.Keyword}'.");
            }

            existing.Description = option.Description;
        }

        foreach (var (keyword, target) in unitRef.TransitionOverrides)
        {
            if (!resolved.AllowedKeywords.Contains(keyword))
            {
                throw new InvalidOperationException($"Routing definition reference '{shared.Id}' cannot override transition for unknown keyword '{keyword}'.");
            }

            resolved.Transitions[keyword] = target;
        }

        if (!string.IsNullOrWhiteSpace(unitRef.ExecutionKind))
        {
            resolved.ExecutionKind = unitRef.ExecutionKind;
        }

        if (!string.IsNullOrWhiteSpace(unitRef.ScriptPath))
        {
            resolved.ScriptPath = unitRef.ScriptPath;
        }

        return resolved;
    }

    private static LogicalUnit CloneSharedUnit(SharedLogicalUnitDefinition shared)
    {
        return new LogicalUnit
        {
            Name = shared.Name,
            Instructions = shared.Instructions,
            AllowedKeywords = [.. shared.AllowedKeywords],
            KeywordOptions =
            [
                .. shared.KeywordOptions.Select(x => new KeywordOption { Keyword = x.Keyword, Description = x.Description })
            ],
            Transitions = new Dictionary<string, string>(shared.Transitions, StringComparer.Ordinal),
            ExecutionKind = shared.ExecutionKind,
            ScriptPath = shared.ScriptPath
        };
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
