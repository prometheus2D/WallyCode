using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("setup", HelpText = "Initializes WallyCode in a target directory.")]
internal sealed class SetupCommandOptions
{
    [Option("source", HelpText = "Target source directory for setup. Defaults to the app folder.")]
    public string? SourcePath { get; set; }

    [Option("vs-build", HelpText = "Resolve the setup target from a standard Visual Studio build output path.")]
    public bool VsBuild { get; set; }

    [Option("cleanup", HelpText = "Runs cleanup first, then recreates setup artifacts.")]
    public bool Cleanup { get; set; }

    [Option("requires-setup", HelpText = "Indicates if a setup environment is required.")]
    public bool RequiresSetup { get; set; }
}