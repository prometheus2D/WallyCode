using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("run", HelpText = "Runs a workflow definition.")]
internal sealed class RunCommandOptions
{
    public const int DefaultMaxRunIterations = 20;

    [Value(0, MetaName = "prompt", Required = false, HelpText = "Prompt for a new workflow session. Omit to continue the active session.")]
    public string? Prompt { get; set; }

    [Option("prompt", HelpText = "Prompt for a new workflow session. Equivalent to the positional prompt.")]
    public string? PromptOption { get; set; }

    [Option("action", HelpText = "Action text for a new workflow session. Equivalent to prompt.")]
    public string? Action { get; set; }

    [Value(1, MetaName = "workflow", Required = false, HelpText = "Workflow definition name. Defaults to 'requirements'.")]
    public string? WorkflowName { get; set; }

    [Option("workflow", HelpText = "Workflow definition name. Overrides the positional workflow name. Defaults to 'requirements'.")]
    public string? Workflow { get; set; }

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for session state.")]
    public string? MemoryRoot { get; set; }

    [Option("max-run-iterations", Default = DefaultMaxRunIterations, HelpText = "Maximum workflow step iterations to execute in this invocation.")]
    public int MaxRunIterations { get; set; } = DefaultMaxRunIterations;

    [Option("max-total-iterations", Default = 0, HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int MaxTotalIterations { get; set; }

    [Option("max-step-repeats", Default = 0, HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public int ResolveMaxRunIterations() => MaxRunIterations;

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

    public string? GetRequestedWorkflowName() =>
        string.IsNullOrWhiteSpace(Workflow) ? WorkflowName : Workflow;
}