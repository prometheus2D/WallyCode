using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("cleanup", HelpText = "Removes WallyCode setup artifacts from a target directory.")]
internal sealed class CleanupCommandOptions
{
    [Option("source", HelpText = "Target source directory for cleanup. Defaults to the active project path.")]
    public string? SourcePath { get; set; }

    [Option("vs-build", HelpText = "Resolve the cleanup target from a standard Visual Studio build output path.")]
    public bool VsBuild { get; set; }

    internal bool PreserveDeployedPayload { get; set; }
}
