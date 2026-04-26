using System.Text;
using System.Text.Json;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Routing;

internal sealed class IterationResult
{
    public required int IterationNumber { get; init; }
    public required string SelectedKeyword { get; init; }
    public required string Summary { get; init; }
    public required string ActiveStepName { get; init; }
    public required string Status { get; init; }
    public required bool StopsInvocation { get; init; }
}

internal sealed class Runner
{
    private const string Continue = "[CONTINUE]";
    private const string AskUser = "[ASK_USER]";
    private const string Done = "[DONE]";
    private const string Error = "[ERROR]";

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

        string keyword;
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

            (keyword, summary) = ParseOutput(rawOutput);

            if (!step.AllowedKeywords.Contains(keyword))
            {
                throw new InvalidOperationException(
                    $"Provider returned keyword '{keyword}' which is not allowed for step '{step.Name}'.");
            }

            (nextStep, status, stops) = ApplyKeyword(step, keyword);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            session.IterationCount++;
            session.LastSelectedKeyword = Error;
            session.LastSummary = ex.Message;
            session.Status = SessionStatus.Error;
            session.PendingResponses.Clear();
            session.Save(_sessionRoot);
            throw;
        }

        session.IterationCount++;
        session.LastSelectedKeyword = keyword;
        session.LastSummary = summary;
        session.ActiveStepName = nextStep;
        session.Status = status;
        session.PendingResponses.Clear();
        session.Save(_sessionRoot);

        return new IterationResult
        {
            IterationNumber = session.IterationCount,
            SelectedKeyword = keyword,
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

    private static string BuildPrompt(Session session, WorkflowStep step, string globalPrompt)
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

        sb.AppendLine("Keyword options:");
        foreach (var keyword in step.AllowedKeywords)
        {
            sb.AppendLine($"  - {keyword}: {step.DescribeKeyword(keyword)}");
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
        sb.AppendLine("Choose the keyword that best matches the next action based on the keyword option descriptions.");
        sb.AppendLine("If an unrecoverable problem occurred, select [ERROR].");
        sb.AppendLine("Respond with strict JSON: { \"selectedKeyword\": \"[ONE_OF_ALLOWED]\", \"summary\": \"...\" }");
        sb.AppendLine("selectedKeyword must be one of the allowed keywords above exactly as written, including brackets. Output JSON only.");
        sb.AppendLine("When selecting [ERROR], put the user-visible reason in summary.");
        return sb.ToString();
    }

    private static string LoadGlobalPrompt(string sessionRoot)
    {
        var projectRoot = Directory.GetParent(sessionRoot)?.FullName;
        return string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot)
            ? string.Empty
            : ProjectSettings.Load(projectRoot).GlobalPrompt ?? string.Empty;
    }

    private static (string keyword, string summary) ParseOutput(string rawOutput)
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

        if (!root.TryGetProperty("selectedKeyword", out var keywordElement) || keywordElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Provider output is missing 'selectedKeyword' string.");
        }

        var summary = root.TryGetProperty("summary", out var summaryElement) && summaryElement.ValueKind == JsonValueKind.String
            ? summaryElement.GetString() ?? string.Empty
            : string.Empty;

        return (keywordElement.GetString()?.Trim() ?? string.Empty, summary);
    }

    private static (string nextStep, string status, bool stops) ApplyKeyword(WorkflowStep step, string keyword)
    {
        if (step.Transitions.TryGetValue(keyword, out var target))
        {
            return (target, SessionStatus.Active, false);
        }

        return keyword switch
        {
            Continue => (step.Name, SessionStatus.Active, false),
            AskUser => (step.Name, SessionStatus.Blocked, true),
            Done => (step.Name, SessionStatus.Completed, true),
            Error => (step.Name, SessionStatus.Error, true),
            _ => throw new InvalidOperationException($"Keyword '{keyword}' has no transition and is not a built-in.")
        };
    }
}
