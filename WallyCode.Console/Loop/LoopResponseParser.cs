using System.Text.Json;

namespace WallyCode.ConsoleApp.Loop;

internal static class LoopResponseParser
{
    public static LoopIterationResponse Parse(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            rawOutput = "Copilot returned no output.";
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
                }) ?? throw new InvalidOperationException("Copilot returned invalid JSON.");

            response.Status = response.Status?.Trim() ?? string.Empty;
            response.Summary = response.Summary?.Trim() ?? string.Empty;
            response.WorkLog = response.WorkLog?.Trim() ?? string.Empty;
            response.DoneReason = response.DoneReason?.Trim() ?? string.Empty;
            response.Questions = NormalizeList(response.Questions);
            response.Decisions = NormalizeList(response.Decisions);
            response.Assumptions = NormalizeList(response.Assumptions);
            response.Blockers = NormalizeList(response.Blockers);
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
        var response = new LoopIterationResponse
        {
            Status = status,
            Summary = summary,
            WorkLog = rawOutput.Trim(),
            Questions = ExtractListSteps(rawOutput),
            Decisions = [],
            Assumptions = [],
            Blockers = [],
            DoneReason = status == "done"
                ? "The loop parser inferred completion from an unstructured Copilot response."
                : string.Empty
        };

        if (response.Questions.Count == 0)
        {
            response.Questions.Add("Review the raw Copilot response and choose the next bounded iteration.");
        }

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
            throw new InvalidOperationException("No JSON object was found in the Copilot response.");
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

        return "Normalized a non-JSON Copilot response into loop memory.";
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

    private static List<string> NormalizeList(IEnumerable<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList()
            ?? [];
    }
}
