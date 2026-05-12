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

    [Option("max-iterations", Default = RunCommandOptions.DefaultMaxIterations, HelpText = "Maximum workflow iterations to run after saving the response.")]
    public int MaxIterations { get; set; } = RunCommandOptions.DefaultMaxIterations;

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
            MaxIterations = MaxIterations,
            Log = Log,
            Verbose = Verbose
        };
    }
}
