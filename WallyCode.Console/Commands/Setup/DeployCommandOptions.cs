using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("deploy", HelpText = "Initializes WallyCode and deploys a local executable into the target source folder.")]
internal sealed class DeployCommandOptions
{
    [Option("source", HelpText = "Target source directory for setup and deployment. Defaults to the app folder.")]
    public string? SourcePath { get; set; }

    [Option("vs-build", HelpText = "Resolve the deployment target from a standard Visual Studio build output path.")]
    public bool VsBuild { get; set; }

    [Option("cleanup", HelpText = "Runs cleanup first, then recreates setup artifacts before deployment.")]
    public bool Cleanup { get; set; }

    [Option("requires-setup", HelpText = "Indicates if a setup environment is required.")]
    public bool RequiresSetup { get; set; }

    public SetupCommandOptions ToSetupOptions()
    {
        return new SetupCommandOptions
        {
            SourcePath = SourcePath,
            VsBuild = VsBuild,
            Cleanup = Cleanup,
            Deploy = true,
            RequiresSetup = RequiresSetup
        };
    }
}