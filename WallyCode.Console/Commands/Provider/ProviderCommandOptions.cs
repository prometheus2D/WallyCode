using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("provider", HelpText = "List providers, list a provider's models, refresh discovered models, or set the default provider and model.")]
internal sealed class ProviderCommandOptions
{
    [Value(0, MetaName = "name", Required = false, HelpText = "Provider name for --set, --models, --refresh, or --model.")]
    public string? Name { get; set; }

    [Option("set", HelpText = "Set the default provider for the current project.")]
    public bool Set { get; set; }

    [Option("models", HelpText = "List supported models for the selected provider.")]
    public bool Models { get; set; }

    [Option("refresh", HelpText = "Refresh and persist discovered models for the selected provider.")]
    public bool Refresh { get; set; }

    [Option("model", HelpText = "Set the default model for the current or selected provider.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }
}
