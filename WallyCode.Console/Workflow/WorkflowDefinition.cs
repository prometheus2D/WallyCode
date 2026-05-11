using System.Text.Json;

namespace WallyCode.ConsoleApp.Workflow;

internal static class StepExecutionKind
{
    public const string Provider = "provider";
    public const string Script = "script";
}

internal sealed class WorkflowTransitionGuard
{
    public string? SelectedStep { get; set; }
    public Dictionary<string, string> MemoryEquals { get; set; } = [];
    public List<string> MemoryExists { get; set; } = [];
    public List<string> MemoryMissing { get; set; } = [];

    public void ValidateShape(string ownerName, string stepName, string selection)
    {
        ValidateMemoryKeys(ownerName, stepName, selection, MemoryEquals.Keys, nameof(MemoryEquals));
        ValidateMemoryKeys(ownerName, stepName, selection, MemoryExists, nameof(MemoryExists));
        ValidateMemoryKeys(ownerName, stepName, selection, MemoryMissing, nameof(MemoryMissing));
    }

    private static void ValidateMemoryKeys(string ownerName, string stepName, string selection, IEnumerable<string> keys, string propertyName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{selection}' contains an empty {propertyName} key.");
            }

            if (!seen.Add(key))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{selection}' contains duplicate {propertyName} key '{key}'.");
            }
        }
    }
}

internal class WorkflowTransition
{
    public string Selection { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetStepName { get; set; }
    public string Status { get; set; } = "active";
    public bool StopsInvocation { get; set; }
    public WorkflowTransitionGuard? Guard { get; set; }

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

        Guard?.ValidateShape(ownerName, stepName, Selection);

        if (Status is not "active" and not "blocked" and not "completed" and not "error")
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' has unsupported status '{Status}'.");
        }

        if (string.Equals(Status, "active", StringComparison.Ordinal))
        {
            if (StopsInvocation)
            {
                throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' cannot stop while keeping workflow status active.");
            }

            return;
        }

        if (!StopsInvocation)
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' with status '{Status}' must stop the invocation.");
        }

        if (!string.IsNullOrWhiteSpace(TargetStepName))
        {
            throw new InvalidOperationException($"Step '{ownerName}/{stepName}' transition '{Selection}' stops the invocation and cannot target another step.");
        }
    }
}

internal class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> ReadsMemory { get; set; } = [];
    public List<string> WritesMemory { get; set; } = [];
    public List<string> TransitionIds { get; set; } = [];
    public List<WorkflowTransition> Transitions { get; set; } = [];
    public string ExecutionKind { get; set; } = StepExecutionKind.Provider;
    public string? ScriptPath { get; set; }
    public string? ScriptArguments { get; set; }
    public int? TimeoutSeconds { get; set; }

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

        ValidateMemoryKeys(ownerName, ReadsMemory, nameof(ReadsMemory));
        ValidateMemoryKeys(ownerName, WritesMemory, nameof(WritesMemory));

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

    private void ValidateMemoryKeys(string ownerName, IEnumerable<string> keys, string propertyName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' contains an empty {propertyName} key.");
            }

            if (!seen.Add(key))
            {
                throw new InvalidOperationException($"Step '{ownerName}/{Name}' contains duplicate {propertyName} key '{key}'.");
            }
        }
    }
}

internal sealed class SharedWorkflowStepDefinition : WorkflowStep
{
    public string Id { get; set; } = string.Empty;
}

internal sealed class SharedWorkflowTransitionDefinition : WorkflowTransition
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
        var transitionsPath = Path.Combine(workflowRoot, "Transitions");

        var sharedTransitions = new Dictionary<string, SharedWorkflowTransitionDefinition>(StringComparer.Ordinal);
        if (Directory.Exists(transitionsPath))
        {
            foreach (var path in Directory.GetFiles(transitionsPath, "*.json", SearchOption.AllDirectories))
            {
                var transition = JsonSerializer.Deserialize<SharedWorkflowTransitionDefinition>(File.ReadAllText(path), JsonOptions.Default)
                    ?? throw new InvalidOperationException($"Shared transition JSON is invalid: {path}");

                if (string.IsNullOrWhiteSpace(transition.Id))
                {
                    throw new InvalidOperationException($"Shared workflow transition id is required: {path}");
                }

                transition.ValidateShape("shared-transition", transition.Id);
                if (!sharedTransitions.TryAdd(transition.Id, transition))
                {
                    throw new InvalidOperationException($"Duplicate shared workflow transition id '{transition.Id}'.");
                }
            }
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

            step.Name = step.Id;
            step.Transitions = ResolveStepTransitions(step, sharedTransitions);

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

    private static List<WorkflowTransition> ResolveStepTransitions(
        SharedWorkflowStepDefinition step,
        IReadOnlyDictionary<string, SharedWorkflowTransitionDefinition> sharedTransitions)
    {
        var transitions = new List<WorkflowTransition>();
        var referencedTransitionIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var transitionId in step.TransitionIds)
        {
            if (string.IsNullOrWhiteSpace(transitionId))
            {
                throw new InvalidOperationException($"Step '{step.Id}' references a transition with no id.");
            }

            if (!referencedTransitionIds.Add(transitionId))
            {
                throw new InvalidOperationException($"Step '{step.Id}' references transition '{transitionId}' more than once.");
            }

            if (!sharedTransitions.TryGetValue(transitionId, out var transition))
            {
                throw new InvalidOperationException($"Step '{step.Id}' references unknown shared transition '{transitionId}'.");
            }

            transitions.Add(CloneTransition(transition));
        }

        transitions.AddRange(step.Transitions.Select(CloneTransition));
        return transitions;
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
            ReadsMemory = [.. shared.ReadsMemory],
            WritesMemory = [.. shared.WritesMemory],
            TransitionIds = [.. shared.TransitionIds],
            Transitions = [.. shared.Transitions.Select(CloneTransition)],
            ExecutionKind = shared.ExecutionKind,
            ScriptPath = shared.ScriptPath,
            ScriptArguments = shared.ScriptArguments,
            TimeoutSeconds = shared.TimeoutSeconds
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
            StopsInvocation = transition.StopsInvocation,
            Guard = CloneGuard(transition.Guard)
        };
    }

    private static WorkflowTransitionGuard? CloneGuard(WorkflowTransitionGuard? guard)
    {
        return guard is null
            ? null
            : new WorkflowTransitionGuard
            {
                SelectedStep = guard.SelectedStep,
                MemoryEquals = new Dictionary<string, string>(guard.MemoryEquals, StringComparer.Ordinal),
                MemoryExists = [.. guard.MemoryExists],
                MemoryMissing = [.. guard.MemoryMissing]
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
