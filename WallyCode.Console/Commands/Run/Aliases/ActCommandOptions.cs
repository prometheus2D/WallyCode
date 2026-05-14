using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("act", HelpText = "Shortcut for run <prompt> act.")]
internal sealed class ActCommandOptions
{
    [Value(0, MetaName = "prompt", Required = false, HelpText = "Prompt for a new act session. Omit to continue the active session.")]
    public string? Prompt { get; set; }

    [Option("prompt", HelpText = "Prompt for a new act session. Equivalent to the positional prompt.")]
    public string? PromptOption { get; set; }

    [Option("action", HelpText = "Action text for a new act session. Equivalent to prompt.")]
    public string? Action { get; set; }

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("max-run-iterations", HelpText = "Maximum workflow step iterations to execute in this invocation.")]
    public int? MaxRunIterations { get; set; }

    [Option("max-total-iterations", HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int? MaxTotalIterations { get; set; }

    [Option("max-step-repeats", HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int? MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose logging for this invocation.")]
    public bool Verbose { get; set; }

    public string? ResolvePrompt()
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

    public RunCommandOptions ToRunOptions() => new()
    {
        Prompt = ResolvePrompt(),
        WorkflowName = "act",
        Provider = Provider,
        Model = Model,
        SourcePath = SourcePath,
        MaxRunIterations = MaxRunIterations,
        MaxTotalIterations = MaxTotalIterations,
        MaxStepRepeats = MaxStepRepeats,
        Log = Log,
        Verbose = Verbose
    };
}
