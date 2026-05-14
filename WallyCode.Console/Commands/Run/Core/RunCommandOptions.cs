using CommandLine;
using WallyCode.ConsoleApp.Project;

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

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context. Overrides and updates the active project path.")]
    public string? SourcePath { get; set; }

    [Option("max-run-iterations", HelpText = "Maximum workflow step iterations to execute in this invocation.")]
    public int? MaxRunIterations { get; set; }

    [Option("max-total-iterations", HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int? MaxTotalIterations { get; set; }

    [Option("max-step-repeats", HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int? MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public int ResolveMaxRunIterations(ProjectSettings settings)
    {
        var maxRunIterations = MaxRunIterations
            ?? settings.RuntimeDefaults.MaxRunIterations
            ?? DefaultMaxRunIterations;

        if (maxRunIterations <= 0)
        {
            throw new InvalidOperationException("Max run iterations must be greater than zero.");
        }

        return maxRunIterations;
    }

    public int ResolveMaxTotalIterations(ProjectSettings settings)
    {
        var maxTotalIterations = MaxTotalIterations
            ?? settings.RuntimeDefaults.MaxTotalIterations
            ?? 0;

        if (maxTotalIterations < 0)
        {
            throw new InvalidOperationException("Max total iterations must be zero (no limit) or greater.");
        }

        return maxTotalIterations;
    }

    public int ResolveMaxStepRepeats(ProjectSettings settings)
    {
        var maxStepRepeats = MaxStepRepeats
            ?? settings.RuntimeDefaults.MaxStepRepeats
            ?? 0;

        if (maxStepRepeats < 0)
        {
            throw new InvalidOperationException("Max step repeats must be zero (no limit) or greater.");
        }

        return maxStepRepeats;
    }

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