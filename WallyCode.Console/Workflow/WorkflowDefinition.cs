using System.Text.Json;

namespace WallyCode.ConsoleApp.Workflow;

internal static class StepExecutionKind
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
            throw new InvalidOperationException($"Workflow step '{ownerName}' contains a keyword option with no keyword.");
        }
    }
}

internal class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> AllowedKeywords { get; set; } = [];
    public List<KeywordOption> KeywordOptions { get; set; } = [];
    public Dictionary<string, string> Transitions { get; set; } = new(StringComparer.Ordinal);
    public string ExecutionKind { get; set; } = StepExecutionKind.Provider;
    public string? ScriptPath { get; set; }

    public string QualifiedName(string workflowName) => $"{workflowName}/{Name}";

    public string DescribeKeyword(string keyword)
    {
        var option = KeywordOptions.FirstOrDefault(x => string.Equals(x.Keyword, keyword, StringComparison.Ordinal));
        return option?.Description ?? string.Empty;
    }

    public void ValidateShape(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Workflow definition '{ownerName}' contains a step with no name.");
        }

        if (string.IsNullOrWhiteSpace(ExecutionKind))
        {
            ExecutionKind = StepExecutionKind.Provider;
        }

        if (!string.Equals(ExecutionKind, StepExecutionKind.Provider, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(ExecutionKind, StepExecutionKind.Script, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Step '{ownerName}/{Name}' has unsupported executionKind '{ExecutionKind}'.");
        }

        if (string.Equals(ExecutionKind, StepExecutionKind.Script, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(ScriptPath))
        {
            throw new InvalidOperationException($"Step '{ownerName}/{Name}' uses executionKind 'script' but has no scriptPath.");
        }

        var describedKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in KeywordOptions)
        {
            option.Validate($"{ownerName}/{Name}");
            if (!describedKeywords.Add(option.Keyword))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' has duplicate keyword option '{option.Keyword}'.");
            }
            if (string.IsNullOrWhiteSpace(option.Description))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' keyword '{option.Keyword}' must have a description.");
            }
        }

        foreach (var keyword in AllowedKeywords)
        {
            if (!describedKeywords.Contains(keyword))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' allowed keyword '{keyword}' must have a keyword option description.");
            }
        }

        foreach (var transitionKey in Transitions.Keys)
        {
            if (!AllowedKeywords.Contains(transitionKey))
            {
                throw new InvalidOperationException($"Step '{Name}' transition '{transitionKey}' is not in allowedKeywords.");
            }

            if (!describedKeywords.Contains(transitionKey))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' transition keyword '{transitionKey}' must have a keyword option description.");
            }
        }
    }
}

internal sealed class SharedWorkflowStepDefinition : WorkflowStep
{
    public string Id { get; set; } = string.Empty;
}

internal sealed class StepReference
{
    public string Ref { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? PromptAddon { get; set; }
    public Dictionary<string, string> TransitionOverrides { get; set; } = new(StringComparer.Ordinal);
    public List<KeywordOption> KeywordOptionOverrides { get; set; } = [];
    public string? ExecutionKind { get; set; }
    public string? ScriptPath { get; set; }
}

internal sealed class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;
    public string StartStepName { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = [];
    public List<StepReference> StepRefs { get; set; } = [];

    public WorkflowStep GetStep(string name) =>
        Steps.FirstOrDefault(step => step.Name == name)
        ?? throw new InvalidOperationException($"Workflow definition '{Name}' has no step '{name}'.");

    public static WorkflowDefinition LoadByName(string workflowName)
    {
        return WorkflowCatalog.LoadFromBaseDirectory().GetDefinition(workflowName);
    }

    public static WorkflowDefinition LoadFromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<WorkflowDefinition>(json, JsonOptions.Default)
            ?? throw new InvalidOperationException("Workflow definition JSON is empty or invalid.");
        definition.Validate();
        return definition;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Workflow definition name is required.");
        }

        if (Steps.Count == 0)
        {
            throw new InvalidOperationException($"Workflow definition '{Name}' must declare at least one step.");
        }

        var stepNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in Steps)
        {
            step.ValidateShape(Name);
            if (!stepNames.Add(step.Name))
            {
                throw new InvalidOperationException($"Workflow definition '{Name}' has duplicate step '{step.Name}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(StartStepName) || !stepNames.Contains(StartStepName))
        {
            throw new InvalidOperationException($"Workflow definition '{Name}' startStepName '{StartStepName}' is not a declared step.");
        }

        foreach (var step in Steps)
        {
            foreach (var (_, target) in step.Transitions)
            {
                if (target.Contains('/', StringComparison.Ordinal))
                {
                    continue;
                }

                if (!stepNames.Contains(target))
                {
                    throw new InvalidOperationException($"Step '{step.Name}' transition targets unknown step '{target}'.");
                }
            }
        }
    }
}

internal sealed class WorkflowCatalog
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions;
    private readonly Dictionary<string, SharedWorkflowStepDefinition> _sharedSteps;
    private readonly Dictionary<string, KeywordDefinition> _keywords;

    private WorkflowCatalog(
        Dictionary<string, WorkflowDefinition> definitions,
        Dictionary<string, SharedWorkflowStepDefinition> sharedSteps,
        Dictionary<string, KeywordDefinition> keywords)
    {
        _definitions = definitions;
        _sharedSteps = sharedSteps;
        _keywords = keywords;
    }

    public static WorkflowCatalog LoadFromBaseDirectory()
    {
        var workflowRoot = Path.Combine(AppContext.BaseDirectory, "Workflow");
        return LoadFromDirectory(workflowRoot);
    }

    public static WorkflowCatalog LoadFromDirectory(string workflowRoot)
    {
        var definitionsPath = Path.Combine(workflowRoot, "Definitions");
        var stepsPath = Path.Combine(workflowRoot, "Steps");
        var keywordsPath = Path.Combine(workflowRoot, "Keywords");

        var keywords = Directory.Exists(keywordsPath)
            ? Directory.GetFiles(keywordsPath, "*.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<KeywordDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Keyword definition JSON is invalid: {path}"))
                .ToDictionary(keyword => keyword.Id, StringComparer.Ordinal)
            : new Dictionary<string, KeywordDefinition>(StringComparer.Ordinal);

        foreach (var keyword in keywords.Values)
        {
            keyword.Validate();
        }

        var sharedSteps = Directory.Exists(stepsPath)
            ? Directory.GetFiles(stepsPath, "*.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<SharedWorkflowStepDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Shared step JSON is invalid: {path}"))
                .ToDictionary(step => step.Id, StringComparer.Ordinal)
            : new Dictionary<string, SharedWorkflowStepDefinition>(StringComparer.Ordinal);

        foreach (var step in sharedSteps.Values)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
            {
                throw new InvalidOperationException("Shared workflow step id is required.");
            }

            if (string.IsNullOrWhiteSpace(step.Name))
            {
                step.Name = step.Id;
            }

            ApplyKeywordDefinitions(step, keywords, $"shared:{step.Id}");
            step.ValidateShape($"shared:{step.Id}");
        }

        var definitions = Directory.Exists(definitionsPath)
            ? Directory.GetFiles(definitionsPath, "*.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<WorkflowDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Workflow definition JSON is invalid: {path}"))
                .ToDictionary(definition => definition.Name, StringComparer.Ordinal)
            : new Dictionary<string, WorkflowDefinition>(StringComparer.Ordinal);

        var catalog = new WorkflowCatalog(definitions, sharedSteps, keywords);
        catalog.ResolveAndValidate();
        return catalog;
    }

    public WorkflowDefinition GetDefinition(string name) =>
        _definitions.TryGetValue(name, out var definition)
            ? definition
            : throw new InvalidOperationException($"Workflow definition '{name}' not found.");

    private void ResolveAndValidate()
    {
        foreach (var definition in _definitions.Values)
        {
            var resolvedSteps = new List<WorkflowStep>();
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var stepRef in definition.StepRefs)
            {
                if (string.IsNullOrWhiteSpace(stepRef.Ref))
                {
                    throw new InvalidOperationException($"Workflow definition '{definition.Name}' contains an empty step ref.");
                }

                if (!_sharedSteps.TryGetValue(stepRef.Ref, out var shared))
                {
                    throw new InvalidOperationException($"Workflow definition '{definition.Name}' references unknown shared step '{stepRef.Ref}'.");
                }

                var resolved = ResolveReferencedStep(shared, stepRef);
                if (!names.Add(resolved.Name))
                {
                    throw new InvalidOperationException($"Workflow definition '{definition.Name}' has duplicate step '{resolved.Name}'.");
                }

                resolvedSteps.Add(resolved);
            }

            foreach (var step in definition.Steps)
            {
                ApplyKeywordDefinitions(step, _keywords, definition.Name);
                step.ValidateShape(definition.Name);
                if (!names.Add(step.Name))
                {
                    throw new InvalidOperationException($"Workflow definition '{definition.Name}' has duplicate step '{step.Name}'.");
                }

                resolvedSteps.Add(step);
            }

            definition.Steps = resolvedSteps;
            definition.Validate();
        }

        var qualifiedNames = _definitions.Values
            .SelectMany(definition => definition.Steps.Select(step => step.QualifiedName(definition.Name)))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var definition in _definitions.Values)
        {
            foreach (var step in definition.Steps)
            {
                foreach (var (_, target) in step.Transitions)
                {
                    if (target.Contains('/', StringComparison.Ordinal) && !qualifiedNames.Contains(target))
                    {
                        throw new InvalidOperationException($"Step '{definition.Name}/{step.Name}' transition targets unknown step '{target}'.");
                    }
                }
            }
        }
    }

    private static void ApplyKeywordDefinitions(WorkflowStep step, IReadOnlyDictionary<string, KeywordDefinition> keywords, string ownerName)
    {
        var optionsByKeyword = step.KeywordOptions.ToDictionary(option => option.Keyword, StringComparer.Ordinal);
        var resolvedOptions = new List<KeywordOption>();

        foreach (var keyword in step.AllowedKeywords)
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
                    throw new InvalidOperationException($"Step '{ownerName}/{step.Name}' keyword '{keyword}' has no description and no shared keyword definition.");
                }

                option.Description = keywordDefinition.Description;
            }

            resolvedOptions.Add(new KeywordOption { Keyword = keyword, Description = option.Description });
        }

        step.KeywordOptions = resolvedOptions;
    }

    private static WorkflowStep ResolveReferencedStep(SharedWorkflowStepDefinition shared, StepReference stepRef)
    {
        var resolved = CloneSharedStep(shared);

        if (!string.IsNullOrWhiteSpace(stepRef.Name))
        {
            resolved.Name = stepRef.Name;
        }

        if (!string.IsNullOrWhiteSpace(stepRef.PromptAddon))
        {
            resolved.Instructions = string.IsNullOrWhiteSpace(resolved.Instructions)
                ? stepRef.PromptAddon.Trim()
                : $"{resolved.Instructions.Trim()}\n\n{stepRef.PromptAddon.Trim()}";
        }

        foreach (var option in stepRef.KeywordOptionOverrides)
        {
            option.Validate($"shared:{shared.Id}");
            var existing = resolved.KeywordOptions.FirstOrDefault(x => string.Equals(x.Keyword, option.Keyword, StringComparison.Ordinal));
            if (existing is null)
            {
                throw new InvalidOperationException($"Workflow definition reference '{shared.Id}' cannot override unknown keyword '{option.Keyword}'.");
            }

            existing.Description = option.Description;
        }

        foreach (var (keyword, target) in stepRef.TransitionOverrides)
        {
            if (!resolved.AllowedKeywords.Contains(keyword))
            {
                throw new InvalidOperationException($"Workflow definition reference '{shared.Id}' cannot override transition for unknown keyword '{keyword}'.");
            }

            resolved.Transitions[keyword] = target;
        }

        if (!string.IsNullOrWhiteSpace(stepRef.ExecutionKind))
        {
            resolved.ExecutionKind = stepRef.ExecutionKind;
        }

        if (!string.IsNullOrWhiteSpace(stepRef.ScriptPath))
        {
            resolved.ScriptPath = stepRef.ScriptPath;
        }

        return resolved;
    }

    private static WorkflowStep CloneSharedStep(SharedWorkflowStepDefinition shared)
    {
        return new WorkflowStep
        {
            Name = shared.Name,
            Instructions = shared.Instructions,
            AllowedKeywords = [.. shared.AllowedKeywords],
            KeywordOptions =
            [
                .. shared.KeywordOptions.Select(option => new KeywordOption { Keyword = option.Keyword, Description = option.Description })
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
