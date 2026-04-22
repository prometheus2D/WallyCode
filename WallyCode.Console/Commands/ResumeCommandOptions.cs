using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("resume", HelpText = "Resumes the active routed session when the current workspace state is resumable.")]
internal sealed class ResumeCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for session state.")]
    public string? MemoryRoot { get; set; }

    [Option("steps", Default = 1, HelpText = "Runs n iterations in this invocation.")]
    public int Steps { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public LoopCommandOptions ToLoopOptions()
    {
        return new LoopCommandOptions
        {
            SourcePath = SourcePath,
            MemoryRoot = MemoryRoot,
            Steps = Steps,
            Log = Log,
            Verbose = Verbose
        };
    }
}
