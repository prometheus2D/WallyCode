using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("shell", HelpText = "Starts an interactive WallyCode shell.")]
internal sealed class ShellCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }
}
