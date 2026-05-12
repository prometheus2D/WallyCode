using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("resume", HelpText = "Resumes the active session when the current workspace state is resumable.")]
internal sealed class ResumeCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for session state.")]
    public string? MemoryRoot { get; set; }

    [Option("max-iterations", Default = RunCommandOptions.DefaultMaxIterations, HelpText = "Maximum workflow iterations to run before stopping.")]
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
