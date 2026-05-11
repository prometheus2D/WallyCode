using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("act", HelpText = "Shortcut for loop --start-step act.")]
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

    [Option("until-complete", HelpText = "Runs until the workflow stops, blocks, errors, or reaches the safety cap of 20 iterations.")]
    public bool UntilComplete { get; set; }

    [Option("log", HelpText = "Enable logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose logging for this invocation.")]
    public bool Verbose { get; set; }

    public LoopCommandOptions ToLoopOptions() => new()
    {
        Goal = Goal,
        StartStepName = "act",
        Provider = Provider,
        Model = Model,
        SourcePath = SourcePath,
        MemoryRoot = MemoryRoot,
        Steps = Steps,
        UntilComplete = UntilComplete,
        Log = Log,
        Verbose = Verbose
    };
}
