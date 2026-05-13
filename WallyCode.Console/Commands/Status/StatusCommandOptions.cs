using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("status", HelpText = "Show the current source, memory root, provider, model, and active session state.")]
internal sealed class StatusCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Override the default .wallycode memory root folder.")]
    public string? MemoryRoot { get; set; }
}
