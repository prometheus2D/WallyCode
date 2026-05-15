using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("install", HelpText = "Installs a local WallyCode executable payload into the target folder.")]
internal sealed class InstallCommandOptions
{
    [Option("source", HelpText = "Target folder for the local WallyCode installation. Defaults to the app folder.")]
    public string? SourcePath { get; set; }

    [Option("vs-build", HelpText = "Resolve the install target from a standard Visual Studio build output path.")]
    public bool VsBuild { get; set; }

    [Option("setup", HelpText = "Runs workspace setup after installing the local executable payload.")]
    public bool Setup { get; set; }
}
