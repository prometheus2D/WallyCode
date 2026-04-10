using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("set-provider", HelpText = "Sets the default provider for the current project.")]
internal sealed class SetProviderCommandOptions
{
	[Value(0, MetaName = "name", Required = true, HelpText = "Provider name.")]
	public string Name { get; set; } = string.Empty;

	[Option("source", HelpText = "Repo or folder path used as the project root.")]
	public string? SourcePath { get; set; }
}