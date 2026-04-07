using System.Text;
using System.Text.Json;

namespace WallyCode.ConsoleApp.Loop;

internal static class LoopResponseParser
{
    public static LoopIterationResponse Parse(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            rawOutput = "Provider returned no output.";
        }

        if (TryParseJson(rawOutput, out var response))
        {
            return response;
        }

        return NormalizeUnstructuredResponse(rawOutput);
    }

    private static bool TryParseJson(string rawOutput, out LoopIterationResponse response)
    {
        try
        {
            var payload = ExtractJson(rawOutput);
            response = JsonSerializer.Deserialize<LoopIterationResponse>(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("The provider returned invalid JSON.");

            response.Status = response.Status?.Trim() ?? string.Empty;
            response.Summary = response.Summary?.Trim() ?? string.Empty;
            response.WorkLog = response.WorkLog?.Trim() ?? string.Empty;
            response.CurrentTasks = response.CurrentTasks?.Trim() ?? string.Empty;
            response.Perspectives = response.Perspectives?.Trim() ?? string.Empty;
            response.NextSteps = response.NextSteps?.Trim() ?? string.Empty;
            response.CurrentState = response.CurrentState?.Trim() ?? string.Empty;
            response.DoneReason = response.DoneReason?.Trim() ?? string.Empty;
            response.Validate();
            return true;
        }
        catch
        {
            response = null!;
            return false;
        }
    }

    private static LoopIterationResponse NormalizeUnstructuredResponse(string rawOutput)
    {
        var summary = ExtractSummary(rawOutput);
        var status = InferStatus(rawOutput);
        var nextSteps = BuildStepsDocument(rawOutput, "# Next Steps", "Review the raw provider output and choose the next bounded iteration.");
        var currentTasks = BuildStepsDocument(rawOutput, "# Current Tasks", "Turn the highest-priority next step into a single bounded work chunk.");
        var response = new LoopIterationResponse
        {
            Status = status,
            Summary = summary,
            WorkLog = rawOutput.Trim(),
            CurrentTasks = currentTasks,
            Perspectives = BuildPerspectivesDocument(rawOutput),
            NextSteps = nextSteps,
            CurrentState = BuildCurrentState(summary, status),
            DoneReason = status == "done"
                ? "The loop parser inferred completion from an unstructured provider response."
                : string.Empty
        };

        response.Validate();
        return response;
    }

    private static string ExtractJson(string rawOutput)
    {
        var trimmed = rawOutput.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = trimmed.IndexOf('\n');

            if (firstLineEnd >= 0)
            {
                trimmed = trimmed[(firstLineEnd + 1)..];
            }

            var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

            if (closingFence >= 0)
            {
                trimmed = trimmed[..closingFence];
            }

            trimmed = trimmed.Trim();
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            throw new InvalidOperationException("No JSON object was found in the provider output.");
        }

        return trimmed[firstBrace..(lastBrace + 1)];
    }

    private static string ExtractSummary(string rawOutput)
    {
        foreach (var rawLine in SplitLines(rawOutput))
        {
            var trimmed = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(trimmed)
                || trimmed.StartsWith("---", StringComparison.Ordinal)
                || trimmed.StartsWith("## ", StringComparison.Ordinal)
                || trimmed.StartsWith("### ", StringComparison.Ordinal)
                || trimmed.StartsWith('|'))
            {
                continue;
            }

            var cleaned = CleanMarkdown(trimmed);

            if (string.IsNullOrWhiteSpace(cleaned) || string.Equals(cleaned, "Agreed Priorities", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var colonIndex = cleaned.IndexOf(':');

            if (colonIndex >= 0 && colonIndex < 24)
            {
                cleaned = cleaned[(colonIndex + 1)..].Trim();
            }

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                continue;
            }

            var sentenceEnd = cleaned.IndexOf('.');
            return sentenceEnd >= 0 ? cleaned[..(sentenceEnd + 1)] : cleaned;
        }

        return "Normalized a non-JSON provider response into loop memory.";
    }

    private static string InferStatus(string rawOutput)
    {
        var lowered = rawOutput.ToLowerInvariant();

        if (lowered.Contains("status: done", StringComparison.Ordinal)
            || lowered.Contains("status = done", StringComparison.Ordinal)
            || lowered.Contains("goal is complete", StringComparison.Ordinal)
            || lowered.Contains("all tasks are complete", StringComparison.Ordinal)
            || lowered.Contains("nothing else remains", StringComparison.Ordinal))
        {
            return "done";
        }

        return "continue";
    }

    private static string BuildStepsDocument(string rawOutput, string title, string fallbackStep)
    {
        var steps = ExtractTableSteps(rawOutput);

        if (steps.Count == 0)
        {
            steps = ExtractListSteps(rawOutput);
        }

        if (steps.Count == 0)
        {
            steps.Add(fallbackStep);
        }

        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine();

        for (var index = 0; index < steps.Count; index++)
        {
            builder.AppendLine($"{index + 1}. {steps[index]}");
        }

        return builder.ToString().TrimEnd();
    }

    private static List<string> ExtractTableSteps(string rawOutput)
    {
        var steps = new List<string>();

        foreach (var rawLine in SplitLines(rawOutput))
        {
            var trimmed = rawLine.Trim();

            if (!trimmed.StartsWith('|') || trimmed.Contains("---", StringComparison.Ordinal))
            {
                continue;
            }

            var cells = trimmed
                .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (cells.Length < 2 || !int.TryParse(cells[0], out _))
            {
                continue;
            }

            var step = CleanMarkdown(cells[1]);

            if (!string.IsNullOrWhiteSpace(step))
            {
                steps.Add(step);
            }
        }

        return steps;
    }

    private static List<string> ExtractListSteps(string rawOutput)
    {
        var steps = new List<string>();

        foreach (var rawLine in SplitLines(rawOutput))
        {
            var trimmed = rawLine.Trim();

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var step = CleanMarkdown(trimmed[2..]);

                if (!string.IsNullOrWhiteSpace(step))
                {
                    steps.Add(step);
                }
            }
        }

        return steps;
    }

    private static string BuildPerspectivesDocument(string rawOutput)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var speakers = new[] { "Will Wright", "Chris Sawyer", "Notch", "Ron" };
        string? currentSpeaker = null;

        foreach (var rawLine in SplitLines(rawOutput))
        {
            var trimmed = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var matchedSpeaker = speakers.FirstOrDefault(speaker =>
                trimmed.StartsWith($"**{speaker}:**", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith($"**{speaker}:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith($"{speaker}:", StringComparison.OrdinalIgnoreCase));

            if (matchedSpeaker is not null)
            {
                currentSpeaker = matchedSpeaker;

                if (!sections.ContainsKey(currentSpeaker))
                {
                    sections[currentSpeaker] = new List<string>();
                }

                var lineContent = CleanMarkdown(trimmed);
                var colonIndex = lineContent.IndexOf(':');

                if (colonIndex >= 0 && colonIndex < lineContent.Length - 1)
                {
                    lineContent = lineContent[(colonIndex + 1)..].Trim();
                }

                if (!string.IsNullOrWhiteSpace(lineContent))
                {
                    sections[currentSpeaker].Add(lineContent);
                }

                continue;
            }

            if (currentSpeaker is null)
            {
                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith('|'))
            {
                continue;
            }

            sections[currentSpeaker].Add(CleanMarkdown(trimmed));
        }

        if (sections.Count == 0)
        {
            return $$"""
# Perspectives

{{rawOutput.Trim()}}
""";
        }

        var builder = new StringBuilder();
        builder.AppendLine("# Perspectives");
        builder.AppendLine();

        foreach (var entry in sections)
        {
            builder.AppendLine($"## {entry.Key}");
            builder.AppendLine();
            builder.AppendLine(string.Join(Environment.NewLine + Environment.NewLine, entry.Value.Where(value => !string.IsNullOrWhiteSpace(value))));
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildCurrentState(string summary, string status)
    {
        return $$"""
# Current State

- Loop status: {{status}}
- Latest summary: {{summary}}
- The provider returned unstructured text, so the application normalized it into memory files.
- The original response is preserved in the raw iteration artifact for inspection.
""";
    }

    private static IEnumerable<string> SplitLines(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }

    private static string CleanMarkdown(string content)
    {
        var cleaned = content
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace("##", string.Empty, StringComparison.Ordinal)
            .Replace("#", string.Empty, StringComparison.Ordinal)
            .Trim();

        while (cleaned.Contains("  ", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("  ", " ", StringComparison.Ordinal);
        }

        return cleaned;
    }
}