using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("prompt", HelpText = "Runs a one-shot prompt using the resolved project provider.")]
internal sealed class PromptCommandOptions
{
    [Value(0, MetaName = "prompt", Required = true, HelpText = "Prompt text to send to the provider.")]
    public string Prompt { get; set; } = string.Empty;

    [Option("provider", HelpText = "Provider preset override. Examples: gh-copilot-claude, gh-copilot-gpt5.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override passed to the provider.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }
}