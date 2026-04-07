using CommandLine;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ShellCommandHandler
{
    public async Task<int> ExecuteAsync(string[] originalArgs, CancellationToken cancellationToken)
    {
        var shellOptions = ParseShellOptions(originalArgs);

        if (shellOptions.ResetMemory)
        {
            ResetMemory(shellOptions);
        }

        Console.WriteLine("WallyCode shell");
        Console.WriteLine("Type a WallyCode command without the executable name.");
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("wallycode> ");
            var input = Console.ReadLine();

            if (input is null)
            {
                return 0;
            }

            var trimmed = input.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (string.Equals(trimmed, "exit", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var args = SplitArguments(trimmed);

            if (args.Length == 0)
            {
                continue;
            }

            if (string.Equals(args[0], "shell", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Already in the shell. Type another command or 'exit'.");
                Console.WriteLine();
                continue;
            }

            if (string.Equals(args[0], "reset-memory", StringComparison.OrdinalIgnoreCase))
            {
                ResetMemory(ParseResetMemoryOptions(args));
                Console.WriteLine();
                continue;
            }

            await Program.RunAsync(args, cancellationToken);
            Console.WriteLine();
        }

        return 0;
    }

    private static void ResetMemory(ShellCommandOptions options)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var resolvedMemoryRoot = string.IsNullOrWhiteSpace(options.MemoryRoot)
            ? null
            : Path.GetFullPath(options.MemoryRoot);

        MemoryWorkspace.Reset(projectRoot, resolvedMemoryRoot);
        Console.WriteLine($"Reset memory workspace at {resolvedMemoryRoot ?? Path.Combine(projectRoot, ".wallycode")}");
        Console.WriteLine("A new loop session will be created the next time you run loop <goal>.");
    }

    private static ShellCommandOptions ParseShellOptions(string[] originalArgs)
    {
        var parser = new Parser(settings =>
        {
            settings.CaseSensitive = false;
            settings.CaseInsensitiveEnumValues = true;
            settings.HelpWriter = TextWriter.Null;
        });

        var result = parser.ParseArguments<ShellCommandOptions>(originalArgs);
        ShellCommandOptions? options = null;

        result.WithParsed(parsed => options = parsed);

        return options ?? new ShellCommandOptions();
    }

    private static ShellCommandOptions ParseResetMemoryOptions(string[] args)
    {
        var parser = new Parser(settings =>
        {
            settings.CaseSensitive = false;
            settings.CaseInsensitiveEnumValues = true;
            settings.HelpWriter = TextWriter.Null;
        });

        var normalizedArgs = args[1..];
        var result = parser.ParseArguments<ShellCommandOptions>(normalizedArgs);
        ShellCommandOptions? options = null;

        result.WithParsed(parsed => options = parsed);

        return options ?? new ShellCommandOptions();
    }

    private static string[] SplitArguments(string commandLine)
    {
        var arguments = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            arguments.Add(current.ToString());
        }

        return arguments.ToArray();
    }
}
