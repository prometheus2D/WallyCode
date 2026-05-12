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

    [Option("max-iterations", Default = RunCommandOptions.DefaultMaxIterations, HelpText = "Maximum workflow iterations to run before stopping.")]
    public int MaxIterations { get; set; } = RunCommandOptions.DefaultMaxIterations;

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
        MaxIterations = MaxIterations,
        Log = Log,
        Verbose = Verbose
    };
}
