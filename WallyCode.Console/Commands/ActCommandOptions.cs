using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("act", HelpText = "Runs the act routing definition as a direct-action workflow.")]
internal sealed class ActCommandOptions
{
    [Value(0, MetaName = "goal", Required = false, HelpText = "Goal for a new act session. Omit to continue the active session.")]
    public string? Goal { get; set; }

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for session state.")]
    public string? MemoryRoot { get; set; }

    [Option("steps", Default = 1, HelpText = "Runs n iterations in this invocation.")]
    public int Steps { get; set; }

    public LoopCommandOptions ToLoopOptions() => new()
    {
        Goal = Goal,
        Definition = "act",
        Provider = Provider,
        Model = Model,
        SourcePath = SourcePath,
        MemoryRoot = MemoryRoot,
        Steps = Steps
    };
}
