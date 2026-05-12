using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("act", HelpText = "Shortcut for run <prompt> act.")]
internal sealed class ActCommandOptions
{
    [Value(0, MetaName = "prompt", Required = false, HelpText = "Prompt for a new act session. Omit to continue the active session.")]
    public string? Prompt { get; set; }

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for session state.")]
    public string? MemoryRoot { get; set; }

    [Option("max-run-iterations", Default = RunCommandOptions.DefaultMaxRunIterations, HelpText = "Maximum workflow step iterations to execute in this invocation.")]
    public int MaxRunIterations { get; set; } = RunCommandOptions.DefaultMaxRunIterations;

    [Option("max-iterations", HelpText = "Deprecated alias for --max-run-iterations.")]
    public int? DeprecatedMaxIterations { get; set; }

    [Option("max-total-iterations", Default = 0, HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int MaxTotalIterations { get; set; }

    [Option("max-step-repeats", Default = 0, HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose logging for this invocation.")]
    public bool Verbose { get; set; }

    public RunCommandOptions ToRunOptions() => new()
    {
        Prompt = Prompt,
        WorkflowName = "act",
        Provider = Provider,
        Model = Model,
        SourcePath = SourcePath,
        MemoryRoot = MemoryRoot,
        MaxRunIterations = DeprecatedMaxIterations ?? MaxRunIterations,
        MaxTotalIterations = MaxTotalIterations,
        MaxStepRepeats = MaxStepRepeats,
        Log = Log,
        Verbose = Verbose
    };
}
