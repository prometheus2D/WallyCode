using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("prompt", HelpText = "Runs a one-shot prompt.")]
internal sealed class PromptCommandOptions
{
    [Value(0, MetaName = "prompt", Required = true, HelpText = "Prompt text to send.")]
    public string Prompt { get; set; } = string.Empty;

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override passed to the Copilot CLI.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and Copilot source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for prompt logs, prompts, raw output, and other runtime artifacts.")]
    public string? MemoryRoot { get; set; }
}