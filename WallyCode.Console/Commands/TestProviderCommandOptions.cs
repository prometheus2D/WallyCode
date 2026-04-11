using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("test-provider", HelpText = "Runs a smoke test using the resolved project provider.")]
internal sealed class TestProviderCommandOptions
{
	[Option("provider", HelpText = "Provider preset override. Examples: gh-copilot-claude, gh-copilot-gpt5.")]
	public string? Provider { get; set; }

	[Option("model", HelpText = "Optional model override passed to the provider.")]
	public string? Model { get; set; }

	[Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
	public string? SourcePath { get; set; }
}