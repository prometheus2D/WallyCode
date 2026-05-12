using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("run", HelpText = "Runs a workflow definition.")]
internal sealed class RunCommandOptions
{
    public const int DefaultMaxIterations = 20;

    [Value(0, MetaName = "prompt", Required = false, HelpText = "Prompt for a new workflow session. Omit to continue the active session.")]
    public string? Prompt { get; set; }

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

    [Option("max-iterations", Default = DefaultMaxIterations, HelpText = "Maximum workflow iterations to run before stopping.")]
    public int MaxIterations { get; set; } = DefaultMaxIterations;

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public string? GetRequestedWorkflowName() =>
        string.IsNullOrWhiteSpace(Workflow) ? WorkflowName : Workflow;
}