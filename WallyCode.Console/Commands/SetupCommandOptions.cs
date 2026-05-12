using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("setup", HelpText = "Initializes WallyCode in a target directory.")]
internal sealed class SetupCommandOptions
{
    [Option("directory", HelpText = "Target directory for setup. Defaults to the app folder.")]
    public string? DirectoryPath { get; set; }

    [Option("vs-build", HelpText = "Resolve the setup target from a standard Visual Studio build output path.")]
    public bool VsBuild { get; set; }

    [Option("force", HelpText = "Recreate setup artifacts even when they already exist.")]
    public bool Force { get; set; }
}