using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

[Verb("tutorial", HelpText = "Lists available tutorials or shows a named tutorial.")]
internal sealed class TutorialCommandOptions
{
    [Value(0, MetaName = "name", Required = false, HelpText = "Optional tutorial name to display.")]
    public string? Name { get; set; }

    [Option("list", HelpText = "Lists available tutorials.")]
    public bool List { get; set; }
}
