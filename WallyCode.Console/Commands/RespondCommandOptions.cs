using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("respond", HelpText = "Appends a user response and resumes the active workflow session.")]
internal sealed class RespondCommandOptions
{
    [Value(0, MetaName = "response", Required = false, HelpText = "Response text to append before resuming the active workflow session.")]
    public string Response { get; set; } = string.Empty;

    [Option("action", HelpText = "Action text to append before resuming. Equivalent to the positional response.")]
    public string? Action { get; set; }

    [Option("prompt", HelpText = "Prompt text to append before resuming. Equivalent to response/action.")]
    public string? Prompt { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for workflow session state.")]
    public string? MemoryRoot { get; set; }

    [Option("max-run-iterations", HelpText = "Maximum workflow step iterations to execute in this invocation after saving the response.")]
    public int? MaxRunIterations { get; set; }

    [Option("max-total-iterations", HelpText = "Maximum total workflow iterations allowed for the active session. Use 0 for no limit.")]
    public int? MaxTotalIterations { get; set; }

    [Option("max-step-repeats", HelpText = "Maximum times the same step may run in one invocation. Use 0 for no limit.")]
    public int? MaxStepRepeats { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public string? ResolveResponse()
    {
        if (!string.IsNullOrWhiteSpace(Response))
        {
            return Response;
        }

        if (!string.IsNullOrWhiteSpace(Action))
        {
            return Action;
        }

        return string.IsNullOrWhiteSpace(Prompt) ? null : Prompt;
    }

    public RunCommandOptions ToRunOptions()
    {
        return new RunCommandOptions
        {
            SourcePath = SourcePath,
            MemoryRoot = MemoryRoot,
            MaxRunIterations = MaxRunIterations,
            Log = Log,
            Verbose = Verbose,
            MaxTotalIterations = MaxTotalIterations,
            MaxStepRepeats = MaxStepRepeats
        };
    }
}
