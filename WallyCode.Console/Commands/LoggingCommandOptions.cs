using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("logging", HelpText = "Configures workspace logging for routed sessions.")]
internal sealed class LoggingCommandOptions
{
    [Option("source", HelpText = "Repo or folder path used as the project root.")]
    public string? SourcePath { get; set; }

    [Option("enable", HelpText = "Enable workspace transcript logging.")]
    public bool Enable { get; set; }

    [Option("disable", HelpText = "Disable workspace transcript logging.")]
    public bool Disable { get; set; }

    [Option("verbose", HelpText = "Enable verbose transcript logging.")]
    public bool Verbose { get; set; }

    [Option("quiet", HelpText = "Disable verbose transcript logging.")]
    public bool Quiet { get; set; }
}
