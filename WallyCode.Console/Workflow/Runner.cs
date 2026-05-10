using System.Text;
using System.Text.Json;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Workflow;

internal sealed class IterationResult
{
    public required int IterationNumber { get; init; }
    public required string SelectedStep { get; init; }
    public required string Summary { get; init; }
    public required string ActiveStepName { get; init; }
    public required string Status { get; init; }
    public required bool StopsInvocation { get; init; }
}

internal sealed class Runner
{
    private const string AskUser = "ask_user";
    private const string Done = "done";
    private const string Error = "error";

    private readonly ILlmProvider _provider;
    private readonly WorkflowDefinition _definition;
    private readonly string _sessionRoot;
    private readonly AppLogger? _logger;
    private readonly string _globalPrompt;

    public Runner(ILlmProvider provider, WorkflowDefinition definition, string sessionRoot, AppLogger? logger = null)
    {
        _provider = provider;
        _definition = definition;
        _sessionRoot = sessionRoot;
        _logger = logger;
        _globalPrompt = LoadGlobalPrompt(sessionRoot);
    }

    public async Task<IterationResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var session = Session.Load(_sessionRoot);

        if (session.WorkflowName != _definition.Name)
        {
            throw new InvalidOperationException(
                $"Session is on workflow '{session.WorkflowName}' but '{_definition.Name}' was supplied.");
        }

        if (session.Status is SessionStatus.Completed or SessionStatus.Error)
        {
            throw new InvalidOperationException($"Session is {session.Status}; nothing to run.");
        }

        string selectedStep;
        string summary;
        string nextStep;
        string status;
        bool stops;

        try
        {
            var step = _definition.GetStep(session.ActiveStepName);
            var prompt = BuildPrompt(session, step, _globalPrompt);
            _logger?.LogExchange("OUT", $"iteration {session.IterationCount + 1} prompt ({step.Name})", prompt);

            var rawOutput = await _provider.ExecuteAsync(
                new CopilotRequest { Prompt = prompt, Model = session.Model, SourcePath = session.SourcePath },
                cancellationToken);

            _logger?.LogExchange("IN", $"iteration {session.IterationCount + 1} response ({step.Name})", rawOutput);

            (selectedStep, summary) = ParseOutput(rawOutput);

            if (!GetAllowedSelections(step).Contains(selectedStep, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Provider returned selected step '{selectedStep}' which is not allowed for step '{step.Name}'.");
            }

            (nextStep, status, stops) = ApplySelection(step, selectedStep);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            session.IterationCount++;
            session.LastSelectedStep = Error;
            session.LastSummary = ex.Message;
            session.Status = SessionStatus.Error;
            session.PendingResponses.Clear();
            session.Save(_sessionRoot);
            throw;
        }

        session.IterationCount++;
        session.LastSelectedStep = selectedStep;
        session.LastSummary = summary;
        session.ActiveStepName = nextStep;
        session.Status = status;
        session.PendingResponses.Clear();
        session.Save(_sessionRoot);

        return new IterationResult
        {
            IterationNumber = session.IterationCount,
            SelectedStep = selectedStep,
            Summary = summary,
            ActiveStepName = nextStep,
            Status = status,
            StopsInvocation = stops
        };
    }

    public async Task<IReadOnlyList<IterationResult>> RunAsync(int steps, CancellationToken cancellationToken)
    {
        var results = new List<IterationResult>();
        for (var i = 0; i < steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RunOnceAsync(cancellationToken);
            results.Add(result);
            if (result.StopsInvocation) break;
        }

        return results;
    }

    private string BuildPrompt(Session session, WorkflowStep step, string globalPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Goal: {session.Goal}");
        sb.AppendLine($"Active step: {step.Name}");
        if (!string.IsNullOrWhiteSpace(globalPrompt))
        {
            sb.AppendLine("Global prompt:");
            sb.AppendLine(globalPrompt);
        }

        if (!string.IsNullOrWhiteSpace(step.Instructions))
        {
            sb.AppendLine($"Instructions: {step.Instructions}");
        }

        sb.AppendLine("Step transitions:");
        foreach (var transition in step.Transitions)
        {
            sb.AppendLine($"  - {transition.Selection}: {DescribeTransition(step, transition)}");
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
        sb.AppendLine("Respond with strict JSON: { \"selectedStep\": \"ONE_OF_ALLOWED\", \"summary\": \"...\" }");
        sb.AppendLine("selectedStep must be one of the allowed selections above exactly as written. Output JSON only.");
        return sb.ToString();
    }

    private IReadOnlyList<string> GetAllowedSelections(WorkflowStep step)
    {
        return [.. step.Transitions.Select(transition => transition.Selection), .. GetTerminalOutcomes().Select(outcome => outcome.Selection)];
    }

    private static IReadOnlyList<(string Selection, string Description)> GetTerminalOutcomes()
    {
        return
        [
            (AskUser, "Ask the user for input that is required before progress can continue."),
            (Done, "The user's goal is complete and no further steps are required."),
            (Error, "An unrecoverable problem prevented the workflow from continuing. Put the user-visible reason in summary.")
        ];
    }

    private string DescribeTransition(WorkflowStep step, WorkflowTransition transition)
    {
        if (!string.IsNullOrWhiteSpace(transition.Description))
        {
            return transition.Description;
        }

        if (!string.IsNullOrWhiteSpace(transition.TargetStepName))
        {
            var nextStep = _definition.GetStep(transition.TargetStepName);
            return string.IsNullOrWhiteSpace(nextStep.Instructions)
                ? $"Move to the '{transition.TargetStepName}' step."
                : $"Move to the '{transition.TargetStepName}' step: {nextStep.Instructions}";
        }

        return string.Equals(transition.Status, SessionStatus.Active, StringComparison.Ordinal)
            ? $"Remain on the '{step.Name}' step."
            : $"Set workflow status to '{transition.Status}'.";
    }

    private static string LoadGlobalPrompt(string sessionRoot)
    {
        var projectRoot = Directory.GetParent(sessionRoot)?.FullName;
        return string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot)
            ? string.Empty
            : ProjectSettings.Load(projectRoot).GlobalPrompt ?? string.Empty;
    }

    private static (string selectedStep, string summary) ParseOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            throw new InvalidOperationException("Provider output was empty.");
        }

        var trimmed = rawOutput.Trim();
        if (trimmed.StartsWith("```"))
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

        return (selectedStepElement.GetString()?.Trim() ?? string.Empty, summary);
    }

    private static (string nextStep, string status, bool stops) ApplySelection(WorkflowStep step, string selectedStep)
    {
        if (string.Equals(selectedStep, AskUser, StringComparison.Ordinal))
        {
            return (step.Name, SessionStatus.Blocked, true);
        }

        if (string.Equals(selectedStep, Done, StringComparison.Ordinal))
        {
            return (step.Name, SessionStatus.Completed, true);
        }

        if (string.Equals(selectedStep, Error, StringComparison.Ordinal))
        {
            return (step.Name, SessionStatus.Error, true);
        }

        var transition = step.Transitions.FirstOrDefault(transition => string.Equals(transition.Selection, selectedStep, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Selected step '{selectedStep}' is not allowed for step '{step.Name}'.");

        return (transition.TargetStepName ?? step.Name, transition.Status, transition.StopsInvocation);
    }
}
