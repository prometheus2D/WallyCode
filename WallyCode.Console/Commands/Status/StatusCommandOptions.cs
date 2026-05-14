using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("status", HelpText = "Show the active source, provider, model, and active session state.")]
internal sealed class StatusCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root. Overrides the active project path.")]
    public string? SourcePath { get; set; }
}
