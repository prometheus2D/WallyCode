using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("uninstall", HelpText = "Removes a local WallyCode executable payload from the target folder.")]
internal sealed class UninstallCommandOptions
{
    [Option("source", HelpText = "Target folder for uninstall. Defaults to the active project path.")]
    public string? SourcePath { get; set; }

    [Option("vs-build", HelpText = "Resolve the uninstall target from a standard Visual Studio build output path.")]
    public bool VsBuild { get; set; }
}
