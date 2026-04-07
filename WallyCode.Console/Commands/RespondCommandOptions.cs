using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("respond", HelpText = "Appends a user response for the active loop session.")]
internal sealed class RespondCommandOptions
{
    [Value(0, MetaName = "response", Required = true, HelpText = "Response text to append to the active loop workspace.")]
    public string Response { get; set; } = string.Empty;

    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for loop memory, prompts, raw output, and logs.")]
    public string? MemoryRoot { get; set; }
}
