using System.Text.Json;

namespace WallyCode.ConsoleApp.Workflow;

internal static class StepExecutionKind
{
    public const string Provider = "provider";
    public const string Script = "script";
}

internal sealed class WorkflowTransition
{
    public string Selection { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetStepName { get; set; }
    public string Status { get; set; } = "active";
    public bool StopsInvocation { get; set; }

    public void ValidateShape(string ownerName, string stepName)
    {
        if (string.IsNullOrWhiteSpace(Selection))
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' contains a transition with no selection.");
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            Status = "active";
        }

        if (Status is not "active" and not "blocked" and not "completed" and not "error")
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' has unsupported status '{Status}'.");
        }

        if (string.IsNullOrWhiteSpace(TargetStepName))
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' must target a declared step.");
        }

        if (!string.Equals(Status, "active", StringComparison.Ordinal) || StopsInvocation)
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' must be an active step route. Terminal outcomes are built in.");
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

        var selections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transition in Transitions)
        {
            transition.ValidateShape(ownerName, Name);

            if (!selections.Add(transition.Selection))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' has duplicate transition selection '{transition.Selection}'.");
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
            foreach (var target in step.Transitions.Select(transition => transition.TargetStepName).Where(target => !string.IsNullOrWhiteSpace(target)))
            {
                if (!stepNames.Contains(target!))
                {
                    throw new InvalidOperationException($"Step '{step.Name}' targets unknown transition step '{target}'.");
                }
            }
        }
    }
}

internal sealed class WorkflowCatalog
{
    private readonly Dictionary<string, SharedWorkflowStepDefinition> _sharedSteps;

    private WorkflowCatalog(
        Dictionary<string, SharedWorkflowStepDefinition> sharedSteps)
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

            step.Name = step.Id;

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
        foreach (var step in _sharedSteps.Values)
        {
            foreach (var target in step.Transitions.Select(transition => transition.TargetStepName).Where(target => !string.IsNullOrWhiteSpace(target)))
            {
                if (!_sharedSteps.ContainsKey(target!))
                {
                    throw new InvalidOperationException($"Step '{step.Name}' targets unknown transition step '{target}'. Transitions must target a loadable shared step id.");
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
            Transitions = [.. shared.Transitions.Select(CloneTransition)],
            ExecutionKind = shared.ExecutionKind,
            ScriptPath = shared.ScriptPath
        };
    }

    private static WorkflowTransition CloneTransition(WorkflowTransition transition)
    {
        return new WorkflowTransition
        {
            Selection = transition.Selection,
            Description = transition.Description,
            TargetStepName = transition.TargetStepName,
            Status = transition.Status,
            StopsInvocation = transition.StopsInvocation
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
