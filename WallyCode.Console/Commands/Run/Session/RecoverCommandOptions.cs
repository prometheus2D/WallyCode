using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("recover", HelpText = "Starts a new workflow run from a terminal session using a recovery instruction.")]
internal sealed class RecoverCommandOptions
{
    [Value(0, MetaName = "action", Required = false, HelpText = "Recovery action text used to start the next run.")]
    public string? ActionText { get; set; }

    [Option("action", HelpText = "Recovery action text used to start the next run.")]
    public string? Action { get; set; }

    [Option("prompt", HelpText = "Recovery prompt text used to start the next run.")]
    public string? Prompt { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("max-run-iterations", HelpText = "Maximum workflow step iterations to execute in this invocation after recovery starts.")]
    public int? MaxRunIterations { get; set; }

    [Option("max-total-iterations", HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int? MaxTotalIterations { get; set; }

    [Option("max-step-repeats", HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int? MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public string? ResolveAction()
    {
        if (!string.IsNullOrWhiteSpace(ActionText))
        {
            return ActionText;
        }

        if (!string.IsNullOrWhiteSpace(Action))
        {
            return Action;
        }

        return string.IsNullOrWhiteSpace(Prompt) ? null : Prompt;
    }

    public RunCommandOptions ToRunOptions(string workflowName, string providerName, string? model)
    {
        return new RunCommandOptions
        {
            Prompt = ResolveAction(),
            WorkflowName = workflowName,
            Provider = providerName,
            Model = model,
            SourcePath = SourcePath,
            MaxRunIterations = MaxRunIterations,
            MaxTotalIterations = MaxTotalIterations,
            MaxStepRepeats = MaxStepRepeats,
            Log = Log,
            Verbose = Verbose
        };
    }
}
