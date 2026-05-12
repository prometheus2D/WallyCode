using System.Text;
using System.Text.Json;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Workflow;

internal sealed class ProviderStepExecutor : IStepExecutor
{
    private const string AskUser = "ask_user";
    private const string Error = "error";

    private readonly ILlmProvider _provider;
    private readonly AppLogger? _logger;

    public ProviderStepExecutor(ILlmProvider provider, AppLogger? logger = null)
    {
        _provider = provider;
        _logger = logger;
    }

    public string ExecutionKind => StepExecutionKind.Provider;

    public async Task<StepExecutionResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
    {
        var prompt = BuildPrompt(context);
        _logger?.LogExchange("OUT", $"iteration {context.Session.IterationCount + 1} prompt ({context.Step.Name})", prompt);

        var rawOutput = await _provider.ExecuteAsync(
            new CopilotRequest { Prompt = prompt, Model = context.Session.Model, SourcePath = context.Session.SourcePath },
            cancellationToken);

        _logger?.LogExchange("IN", $"iteration {context.Session.IterationCount + 1} response ({context.Step.Name})", rawOutput);
        return ParseOutput(rawOutput);
    }

    private static string BuildPrompt(StepExecutionContext context)
    {
        var session = context.Session;
        var step = context.Step;
        var sb = new StringBuilder();
        sb.AppendLine($"Goal: {session.Goal}");
        sb.AppendLine($"Workflow: {context.Definition.Name}");
        if (!string.IsNullOrWhiteSpace(context.Definition.Instructions))
        {
            sb.AppendLine("Workflow instructions:");
            sb.AppendLine(context.Definition.Instructions);
        }

        sb.AppendLine($"Active step: {step.Name}");
        if (!string.IsNullOrWhiteSpace(context.GlobalPrompt))
        {
            sb.AppendLine("Global prompt:");
            sb.AppendLine(context.GlobalPrompt);
        }

        if (!string.IsNullOrWhiteSpace(step.Instructions))
        {
            sb.AppendLine($"Instructions: {step.Instructions}");
        }

        AppendSessionMemory(sb, session, step);
        AppendMemoryContract(sb, step);

        sb.AppendLine("Step transitions:");
        foreach (var transition in step.Transitions)
        {
            sb.AppendLine($"  - {transition.Selection}: {DescribeTransition(context.Definition, step, transition)}{DescribeHandoffRequirements(context.Definition, step, transition)}{DescribeGuard(transition.Guard)}");
        }

        sb.AppendLine("Terminal outcomes:");
        sb.AppendLine("These outcomes stop this invocation and do not target workflow steps.");
        foreach (var terminalOutcome in GetTerminalOutcomes())
        {
            sb.AppendLine($"  - {terminalOutcome.Selection}: {terminalOutcome.Description}");
        }

        if (session.PendingResponses.Count > 0)
        {
            sb.AppendLine("User responses since last run:");
            foreach (var response in session.PendingResponses)
            {
                sb.AppendLine($"  - {response}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Choose exactly one step transition or terminal outcome that best matches what should happen next.");
        sb.AppendLine("Respond with strict JSON: { \"selectedStep\": \"ONE_OF_ALLOWED\", \"summary\": \"...\", \"memory\": { \"KEY\": \"VALUE\" } }");
        sb.AppendLine("selectedStep must be one of the allowed selections above exactly as written. Output JSON only.");
        return sb.ToString();
    }

    private static void AppendSessionMemory(StringBuilder sb, Session session, WorkflowStep step)
    {
        if (step.ReadsMemory.Count == 0)
        {
            return;
        }

        sb.AppendLine("Session memory:");
        foreach (var key in step.ReadsMemory)
        {
            var value = session.Memory.TryGetValue(key, out var storedValue) && !string.IsNullOrWhiteSpace(storedValue)
                ? storedValue
                : "[not set]";
            sb.AppendLine($"  - {key}: {FormatMemoryValue(value)}");
        }
    }

    private static void AppendMemoryContract(StringBuilder sb, WorkflowStep step)
    {
        if (step.WritesMemory.Count == 0)
        {
            return;
        }

        sb.AppendLine("Memory this step can update:");
        foreach (var key in step.WritesMemory)
        {
            sb.AppendLine($"  - {key}");
        }
        sb.AppendLine("Put durable context for later steps in the optional top-level memory object. Use null to remove a memory key.");
    }

    private static string FormatMemoryValue(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\n    ", StringComparison.Ordinal);
    }

    private static IReadOnlyList<(string Selection, string Description)> GetTerminalOutcomes()
    {
        return
        [
            (AskUser, "Ask the user for input that is required before progress can continue."),
            (Error, "An unrecoverable problem prevented the workflow from continuing. Put the user-visible reason in summary.")
        ];
    }

    private static string DescribeTransition(WorkflowDefinition definition, WorkflowStep step, WorkflowTransition transition)
    {
        if (!string.IsNullOrWhiteSpace(transition.Description))
        {
            return transition.Description;
        }

        if (!string.IsNullOrWhiteSpace(transition.TargetStepName))
        {
            var nextStep = definition.GetStep(transition.TargetStepName);
            return string.IsNullOrWhiteSpace(nextStep.Instructions)
                ? $"Move to the '{transition.TargetStepName}' step."
                : $"Move to the '{transition.TargetStepName}' step: {nextStep.Instructions}";
        }

        return string.Equals(transition.Status, SessionStatus.Active, StringComparison.Ordinal)
            ? $"Remain on the '{step.Name}' step."
            : $"Set workflow status to '{transition.Status}'.";
    }

    private static string DescribeGuard(WorkflowTransitionGuard? guard)
    {
        if (guard is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(guard.SelectedStep))
        {
            parts.Add($"selectedStep == {guard.SelectedStep}");
        }

        parts.AddRange(guard.MemoryExists.Select(key => $"memory.{key} exists"));
        parts.AddRange(guard.MemoryMissing.Select(key => $"memory.{key} is missing"));
        parts.AddRange(guard.MemoryEquals.Select(entry => $"memory.{entry.Key} == {entry.Value}"));
        return parts.Count == 0
            ? " Guarded transition."
            : $" Guard: {string.Join("; ", parts)}.";
    }

    private static string DescribeHandoffRequirements(WorkflowDefinition definition, WorkflowStep step, WorkflowTransition transition)
    {
        if (string.IsNullOrWhiteSpace(transition.TargetStepName)
            || string.Equals(transition.TargetStepName, step.Name, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var targetStep = definition.GetStep(transition.TargetStepName);
        var handoffKeys = targetStep.ReadsMemory
            .Where(key => step.WritesMemory.Contains(key, StringComparer.Ordinal))
            .ToList();

        return handoffKeys.Count == 0
            ? string.Empty
            : $" Requires memory: {string.Join(", ", handoffKeys)}.";
    }

    private static StepExecutionResult ParseOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            throw new InvalidOperationException("Provider output was empty.");
        }

        var trimmed = rawOutput.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var newlineIndex = trimmed.IndexOf('\n');
            if (newlineIndex >= 0)
            {
                trimmed = trimmed[(newlineIndex + 1)..];
            }

            var fenceIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceIndex >= 0)
            {
                trimmed = trimmed[..fenceIndex];
            }

            trimmed = trimmed.Trim();
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            throw new InvalidOperationException("No JSON object found in provider output.");
        }

        using var document = JsonDocument.Parse(trimmed[firstBrace..(lastBrace + 1)]);
        var root = document.RootElement;

        if (!root.TryGetProperty("selectedStep", out var selectedStepElement) || selectedStepElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Provider output is missing 'selectedStep' string.");
        }

        var summary = root.TryGetProperty("summary", out var summaryElement) && summaryElement.ValueKind == JsonValueKind.String
            ? summaryElement.GetString() ?? string.Empty
            : string.Empty;

        return new StepExecutionResult
        {
            SelectedStep = selectedStepElement.GetString()?.Trim() ?? string.Empty,
            Summary = summary,
            MemoryUpdates = ParseMemoryUpdates(root)
        };
    }

    private static Dictionary<string, string?> ParseMemoryUpdates(JsonElement root)
    {
        var memoryUpdates = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!root.TryGetProperty("memory", out var memoryElement) || memoryElement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return memoryUpdates;
        }

        if (memoryElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Provider output 'memory' must be a JSON object when provided.");
        }

        foreach (var property in memoryElement.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(property.Name))
            {
                throw new InvalidOperationException("Provider output 'memory' contains an empty key.");
            }

            memoryUpdates[property.Name] = property.Value.ValueKind == JsonValueKind.Null
                ? null
                : property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.GetRawText();
        }

        return memoryUpdates;
    }
}