using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("providers", HelpText = "Lists available provider presets and whether each one is ready to run.")]
internal sealed class ProvidersCommandOptions
{
	[Option("source", HelpText = "Repo or folder path used as the project root.")]
	public string? SourcePath { get; set; }
}