using System.Text;
using System.Text.Json;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Routing;

internal sealed class IterationResult
{
    public required int IterationNumber { get; init; }
    public required string SelectedKeyword { get; init; }
    public required string Summary { get; init; }
    public required string ActiveUnitName { get; init; }
    public required string Status { get; init; }
    public required bool StopsInvocation { get; init; }
}

internal sealed class RoutedRunner
{
    private const string Continue = "[CONTINUE]";
    private const string AskUser = "[ASK_USER]";
    private const string Done = "[DONE]";
    private const string Fail = "[FAIL]";

    private readonly ILlmProvider _provider;
    private readonly RoutingDefinition _definition;
    private readonly string _sessionRoot;
    private readonly AppLogger? _logger;
    private readonly string _globalPrompt;

    public RoutedRunner(ILlmProvider provider, RoutingDefinition definition, string sessionRoot, AppLogger? logger = null)
    {
        _provider = provider;
        _definition = definition;
        _sessionRoot = sessionRoot;
        _logger = logger;
        _globalPrompt = LoadGlobalPrompt(sessionRoot);
    }

    public async Task<IterationResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var session = RoutedSession.Load(_sessionRoot);

        if (session.DefinitionName != _definition.Name)
        {
            throw new InvalidOperationException(
                $"Session is on definition '{session.DefinitionName}' but '{_definition.Name}' was supplied.");
        }
        if (session.Status is SessionStatus.Completed or SessionStatus.Failed)
        {
            throw new InvalidOperationException($"Session is {session.Status}; nothing to run.");
        }

        var unit = _definition.GetUnit(session.ActiveUnitName);
        var prompt = BuildPrompt(session, unit, _globalPrompt);
        _logger?.LogExchange("OUT", $"iteration {session.IterationCount + 1} prompt ({unit.Name})", prompt);

        var rawOutput = await _provider.ExecuteAsync(
            new CopilotRequest { Prompt = prompt, Model = session.Model, SourcePath = session.SourcePath },
            cancellationToken);

        _logger?.LogExchange("IN", $"iteration {session.IterationCount + 1} response ({unit.Name})", rawOutput);

        var (keyword, summary) = ParseOutput(rawOutput);

        if (!unit.AllowedKeywords.Contains(keyword))
        {
            throw new InvalidOperationException(
                $"Provider returned keyword '{keyword}' which is not allowed for unit '{unit.Name}'.");
        }

        var (nextUnit, status, stops) = ApplyKeyword(unit, keyword);

        session.IterationCount++;
        session.LastSelectedKeyword = keyword;
        session.ActiveUnitName = nextUnit;
        session.Status = status;
        session.PendingResponses.Clear();
        session.Save(_sessionRoot);

        return new IterationResult
        {
            IterationNumber = session.IterationCount,
            SelectedKeyword = keyword,
            Summary = summary,
            ActiveUnitName = nextUnit,
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

    private static string BuildPrompt(RoutedSession session, LogicalUnit unit, string globalPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Goal: {session.Goal}");
        sb.AppendLine($"Active unit: {unit.Name}");
        if (!string.IsNullOrWhiteSpace(globalPrompt))
        {
            sb.AppendLine("Global prompt:");
            sb.AppendLine(globalPrompt);
        }
        if (!string.IsNullOrWhiteSpace(unit.Instructions))
        {
            sb.AppendLine($"Instructions: {unit.Instructions}");
        }
        sb.AppendLine("Keyword options:");
        foreach (var keyword in unit.AllowedKeywords)
        {
            sb.AppendLine($"  - {keyword}: {unit.DescribeKeyword(keyword)}");
        }

        if (session.PendingResponses.Count > 0)
        {
            sb.AppendLine("User responses since last run:");
            foreach (var r in session.PendingResponses) sb.AppendLine($"  - {r}");
        }

        sb.AppendLine();
        sb.AppendLine("Choose the keyword that best matches the next action based on the keyword option descriptions.");
        sb.AppendLine("Respond with strict JSON: { \"selectedKeyword\": \"[ONE_OF_ALLOWED]\", \"summary\": \"...\" }");
        sb.AppendLine("selectedKeyword must be one of the allowed keywords above exactly as written, including brackets. Output JSON only.");
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
            var nl = trimmed.IndexOf('\n');
            if (nl >= 0) trimmed = trimmed[(nl + 1)..];
            var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0) trimmed = trimmed[..fence];
            trimmed = trimmed.Trim();
        }

        var first = trimmed.IndexOf('{');
        var last = trimmed.LastIndexOf('}');
        if (first < 0 || last <= first)
        {
            throw new InvalidOperationException("No JSON object found in provider output.");
        }

        using var doc = JsonDocument.Parse(trimmed[first..(last + 1)]);
        var root = doc.RootElement;

        if (!root.TryGetProperty("selectedKeyword", out var keywordElement) || keywordElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Provider output is missing 'selectedKeyword' string.");
        }

        var summary = root.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String
            ? s.GetString() ?? string.Empty
            : string.Empty;

        return (keywordElement.GetString()?.Trim() ?? string.Empty, summary);
    }

    private static (string nextUnit, string status, bool stops) ApplyKeyword(LogicalUnit unit, string keyword)
    {
        if (unit.Transitions.TryGetValue(keyword, out var target))
        {
            return (target, SessionStatus.Active, false);
        }
        return keyword switch
        {
            Continue => (unit.Name, SessionStatus.Active, false),
            AskUser => (unit.Name, SessionStatus.Blocked, true),
            Done => (unit.Name, SessionStatus.Completed, true),
            Fail => (unit.Name, SessionStatus.Failed, true),
            _ => throw new InvalidOperationException($"Keyword '{keyword}' has no transition and is not a built-in.")
        };
    }
}
