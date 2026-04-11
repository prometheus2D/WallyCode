using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("provider", HelpText = "List providers or set the active provider.")]
internal sealed class ProviderCommandOptions
{
    [Value(0, MetaName = "name", Required = false, HelpText = "Provider name for --set.")]
    public string? Name { get; set; }

    [Option("set", HelpText = "Set the active provider for the current project.")]
    public bool Set { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }
}
