using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("step", HelpText = "Runs one shared workflow step directly.")]
internal sealed class StepCommandOptions
{
    [Value(0, MetaName = "prompt", Required = false, HelpText = "Prompt for the step execution.")]
    public string? Prompt { get; set; }

    [Option("prompt", HelpText = "Prompt for the step execution. Equivalent to the positional prompt.")]
    public string? PromptOption { get; set; }

    [Option("action", HelpText = "Action text for the step execution. Equivalent to prompt.")]
    public string? Action { get; set; }

    [Value(1, MetaName = "step", Required = false, HelpText = "Shared step id. Defaults to 'ask'.")]
    public string? StepName { get; set; }

    [Option("step", HelpText = "Shared step id. Overrides the positional step name. Defaults to 'ask'.")]
    public string? Step { get; set; }

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder to read session memory from and store logs in.")]
    public string? MemoryRoot { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public string? GetRequestedPrompt()
    {
        if (!string.IsNullOrWhiteSpace(Prompt))
        {
            return Prompt;
        }

        if (!string.IsNullOrWhiteSpace(PromptOption))
        {
            return PromptOption;
        }

        return string.IsNullOrWhiteSpace(Action) ? null : Action;
    }

    public string? GetRequestedStepName() =>
        string.IsNullOrWhiteSpace(Step) ? StepName : Step;
}