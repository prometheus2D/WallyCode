using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("loop", HelpText = "Runs the routing engine against a routing definition.")]
internal sealed class LoopCommandOptions
{
    [Value(0, MetaName = "goal", Required = false, HelpText = "Goal for a new session. Omit to continue the active session.")]
    public string? Goal { get; set; }

    [Option("definition", HelpText = "Routing definition name. Defaults to 'requirements'.")]
    public string? Definition { get; set; }

    [Option("provider", HelpText = "Optional provider override.")]
    public string? Provider { get; set; }

    [Option("model", HelpText = "Optional model override.")]
    public string? Model { get; set; }

    [Option("source", HelpText = "Repo or folder path used as the project root and provider source context.")]
    public string? SourcePath { get; set; }

    [Option("memory-root", HelpText = "Optional folder for session state.")]
    public string? MemoryRoot { get; set; }

    [Option("steps", Default = 1, HelpText = "Runs n iterations in this invocation.")]
    public int Steps { get; set; }

    [Option("step", HelpText = "Runs exactly one iteration in this invocation.")]
    public bool Step { get; set; }

    [Option("log", HelpText = "Enable transcript logging for this invocation.")]
    public bool Log { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging for this invocation.")]
    public bool Verbose { get; set; }

    public int GetEffectiveSteps() => Step ? 1 : Steps;
}
