using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("loop", HelpText = "Runs the iterative memory loop.")]
internal sealed class LoopCommandOptions
{
    [Value(0, MetaName = "goal", Required = false, HelpText = "Goal text for a new loop session. Omit it to continue the active session.")]
    public string? Goal { get; set; }

    [Option("provider", HelpText = "Provider preset override. Examples: gh-copilot-claude, gh-copilot-gpt5.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override passed to the provider.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for loop memory, prompts, raw output, and logs.")]
    public string? MemoryRoot { get; set; }

    [Option("steps", Default = 1, HelpText = "Runs n iterations in this invocation.")]
    public int Steps { get; set; }

    [Option("template", HelpText = "Loop template id. Examples: default, requirements.")]
    public string? Template { get; set; }
}