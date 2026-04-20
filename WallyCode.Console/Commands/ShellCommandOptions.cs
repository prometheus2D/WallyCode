using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("shell", HelpText = "Starts an interactive WallyCode shell.")]
internal sealed class ShellCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for loop session state.")]
    public string? MemoryRoot { get; set; }

    [Option("reset-memory", HelpText = "Deletes the existing memory workspace and recreates it before starting the shell.")]
    public bool ResetMemory { get; set; }

    [Option("log", HelpText = "Enable transcript logging for commands run inside the shell.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for commands run inside the shell.")]
    public bool Verbose { get; set; }
}
