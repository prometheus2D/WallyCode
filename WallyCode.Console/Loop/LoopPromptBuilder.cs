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

User responses document:
{{snapshot.UserResponses}}
""";
    }
}