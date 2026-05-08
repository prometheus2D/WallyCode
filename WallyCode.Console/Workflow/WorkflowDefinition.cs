using System.Text.Json;

namespace WallyCode.ConsoleApp.Workflow;

internal static class StepExecutionKind
{
    public const string Provider = "provider";
    public const string Script = "script";
}

internal sealed class WorkflowTransition
{
    public string Keyword { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? NextStep { get; set; }

    public void Validate(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(Keyword))
        {
            throw new InvalidOperationException($"Workflow step '{ownerName}' contains a transition with no keyword.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new InvalidOperationException($"Workflow step '{ownerName}' transition '{Keyword}' must have a description.");
        }
    }
}

internal class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<WorkflowTransition> Transitions { get; set; } = [];
    public string ExecutionKind { get; set; } = StepExecutionKind.Provider;
    public string? ScriptPath { get; set; }

    public string QualifiedName(string workflowName) => $"{workflowName}/{Name}";

    public IReadOnlyList<string> AllowedKeywords => [.. Transitions.Select(transition => transition.Keyword)];

    public WorkflowTransition? FindTransition(string keyword) =>
        Transitions.FirstOrDefault(transition => string.Equals(transition.Keyword, keyword, StringComparison.Ordinal));

    public string DescribeKeyword(string keyword)
    {
        return FindTransition(keyword)?.Description ?? string.Empty;
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

        if (Transitions.Count == 0)
        {
            throw new InvalidOperationException($"Workflow step '{ownerName}/{Name}' must declare at least one transition.");
        }

        var transitionKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transition in Transitions)
        {
            transition.Validate($"{ownerName}/{Name}");
            if (!transitionKeywords.Add(transition.Keyword))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' has duplicate transition keyword '{transition.Keyword}'.");
            }
        }
    }
}

internal sealed class SharedWorkflowStepDefinition : WorkflowStep
{
    public string Id { get; set; } = string.Empty;
}

internal sealed class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;
    public string StartStepName { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = [];

    public WorkflowStep GetStep(string name) =>
        Steps.FirstOrDefault(step => step.Name == name)
        ?? throw new InvalidOperationException($"Workflow definition '{Name}' has no step '{name}'.");

    public static WorkflowDefinition LoadByName(string workflowName)
    {
        return WorkflowCatalog.LoadFromBaseDirectory().GetDefinition(workflowName);
    }

    public static string NormalizeStartStepName(string name) => WorkflowCatalog.ResolveStartStepName(name);

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
            foreach (var target in step.Transitions.Select(transition => transition.NextStep).Where(target => !string.IsNullOrWhiteSpace(target)))
            {
                if (target!.Contains('/', StringComparison.Ordinal))
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
    private readonly Dictionary<string, SharedWorkflowStepDefinition> _sharedSteps;

    private WorkflowCatalog(Dictionary<string, SharedWorkflowStepDefinition> sharedSteps)
    {
        _sharedSteps = sharedSteps;
    }

    public static WorkflowCatalog LoadFromBaseDirectory()
    {
        var workflowRoot = Path.Combine(AppContext.BaseDirectory, "Workflow");
        return LoadFromDirectory(workflowRoot);
    }

    public static WorkflowCatalog LoadFromDirectory(string workflowRoot)
    {
        var stepsPath = Path.Combine(workflowRoot, "Steps");

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

            step.ValidateShape($"shared:{step.Id}");
        }

        var catalog = new WorkflowCatalog(sharedSteps);
        catalog.ResolveAndValidate();
        return catalog;
    }

    public WorkflowDefinition GetDefinition(string name)
    {
        var startStepName = ResolveStartStepName(name);
        if (!_sharedSteps.ContainsKey(startStepName))
        {
            throw new InvalidOperationException($"Workflow step '{name}' not found.");
        }

        var definition = new WorkflowDefinition
        {
            Name = startStepName,
            StartStepName = startStepName,
            Steps = [.. _sharedSteps.Values.Select(CloneSharedStep)]
        };
        definition.Validate();
        return definition;
    }

    private void ResolveAndValidate()
    {
        var stepNames = _sharedSteps.Values
            .Select(step => step.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var step in _sharedSteps.Values)
        {
            foreach (var target in step.Transitions.Select(transition => transition.NextStep).Where(target => !string.IsNullOrWhiteSpace(target)))
            {
                if (target!.Contains('/', StringComparison.Ordinal))
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

    internal static string ResolveStartStepName(string name)
    {
        return name switch
        {
            "requirements" or "full-pipeline" => "collect_requirements",
            "tasks" => "produce_tasks",
            _ => name
        };
    }

    private static WorkflowStep CloneSharedStep(SharedWorkflowStepDefinition shared)
    {
        return new WorkflowStep
        {
            Name = shared.Name,
            Instructions = shared.Instructions,
            Transitions =
            [
                .. shared.Transitions.Select(transition => new WorkflowTransition
                {
                    Keyword = transition.Keyword,
                    Description = transition.Description,
                    NextStep = transition.NextStep
                })
            ],
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
