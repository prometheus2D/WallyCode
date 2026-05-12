using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("respond", HelpText = "Appends a user response and resumes the active workflow session.")]
internal sealed class RespondCommandOptions
{
    [Value(0, MetaName = "response", Required = true, HelpText = "Response text to append before resuming the active workflow session.")]
    public string Response { get; set; } = string.Empty;

    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for workflow session state.")]
    public string? MemoryRoot { get; set; }

    [Option("max-run-iterations", Default = RunCommandOptions.DefaultMaxRunIterations, HelpText = "Maximum workflow step iterations to execute in this invocation after saving the response.")]
    public int MaxRunIterations { get; set; } = RunCommandOptions.DefaultMaxRunIterations;

    [Option("max-iterations", HelpText = "Deprecated alias for --max-run-iterations.")]
    public int? DeprecatedMaxIterations { get; set; }

    [Option("max-total-iterations", Default = 0, HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int MaxTotalIterations { get; set; }

    [Option("max-step-repeats", Default = 0, HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public RunCommandOptions ToRunOptions()
    {
        return new RunCommandOptions
        {
            SourcePath = SourcePath,
            MemoryRoot = MemoryRoot,
            MaxRunIterations = DeprecatedMaxIterations ?? MaxRunIterations,
            Log = Log,
            Verbose = Verbose,
            MaxTotalIterations = MaxTotalIterations,
            MaxStepRepeats = MaxStepRepeats
        };
    }
}
