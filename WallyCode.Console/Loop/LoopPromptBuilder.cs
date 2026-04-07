using System.Text;
using WallyCode.ConsoleApp.App;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Loop;

internal static class LoopPromptBuilder
{
    public static string Build(AppOptions options, MemoryWorkspace workspace, MemorySnapshot snapshot, int iteration, int step, LoopTemplate template)
    {
        return $$"""
{{template.SystemPrompt}}

Loop metadata:
- Session iteration: {{iteration}}
- Step in this invocation: {{step}} of {{options.MaxIterations}}
- Provider: {{options.ProviderName}}
- Model: {{options.Model ?? "Default"}}
- Source path: {{options.SourcePath ?? "Not provided"}}
- Memory root: {{workspace.RootPath}}
- Loop template: {{template.TemplateId}}
- Stop keyword: {{template.StopKeyword ?? "Not configured"}}

{{template.ResponseSchemaPrompt}}

Return JSON only. No markdown fence. No preamble. No explanation outside the JSON object.
Use this exact schema:
{
  "status": "continue" | "done",
  "summary": "one sentence",
  "workLog": "markdown",
  "questions": ["question or next action"],
  "decisions": ["decision"],
  "assumptions": ["assumption"],
  "blockers": ["blocker"],
  "doneReason": "empty string unless status is done"
}

Rules:
- Keep questions concrete and immediately actionable.
- Return only compact structured data.
- Do not regenerate full memory documents.
- Your first output character must be {.
- Your last output character must be }.

Goal document:
{{snapshot.Goal}}

Current tasks document:
{{snapshot.CurrentTasks}}

Perspectives document:
{{snapshot.Perspectives}}

Next steps document:
{{snapshot.NextSteps}}

Current state document:
{{snapshot.CurrentState}}

Pending user responses since the last processed response:
{{RenderPendingResponses(snapshot.PendingUserResponses)}}
""";
    }

    private static string RenderPendingResponses(IReadOnlyList<UserResponseEntry> responses)
    {
        if (responses.Count == 0)
        {
            return "- None";
        }

        var builder = new StringBuilder();

        foreach (var response in responses)
        {
            builder.AppendLine($"## Response {response.Id} | {response.TimestampUtc:yyyy-MM-dd HH:mm:ss zzz}");
            builder.AppendLine();
            builder.AppendLine(response.Text);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}