using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ShellCommandHandler
{
    private readonly ShellCommandOptions _options;

    public ShellCommandHandler(ShellCommandOptions options)
    {
        _options = options;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_options.ResetMemory)
        {
            ResetMemory(_options);
        }

        Console.WriteLine("WallyCode shell");
        Console.WriteLine("Type a WallyCode command without the executable name.");
        Console.WriteLine("Type 'exit' to quit.");

        var resolvedSourcePath = ProjectSettings.ResolveProjectRoot(_options.SourcePath);
        Console.WriteLine($"Shell initialized with source: {resolvedSourcePath}");

        if (!string.IsNullOrWhiteSpace(_options.MemoryRoot))
        {
            Console.WriteLine($"Shell initialized with memory root: {Path.GetFullPath(_options.MemoryRoot)}");
        }
        else
        {
            Console.WriteLine($"Shell using default memory root: {Path.Combine(resolvedSourcePath, ".wallycode")}");
        }

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
                ResetMemory(_options);
                Console.WriteLine();
                continue;
            }

            var effectiveArgs = ApplyShellDefaults(args);
            await Program.RunAsync(effectiveArgs, cancellationToken);
            Console.WriteLine();
        }

        return 0;
    }

    private string[] ApplyShellDefaults(string[] args)
    {
        var effectiveArgs = args.ToList();

        if (!HasOption(args, "source") && !string.IsNullOrWhiteSpace(_options.SourcePath))
        {
            effectiveArgs.Add("--source");
            effectiveArgs.Add(_options.SourcePath);
        }

        if (SupportsMemoryRoot(args[0])
            && !HasOption(args, "memory-root")
            && !string.IsNullOrWhiteSpace(_options.MemoryRoot))
        {
            effectiveArgs.Add("--memory-root");
            effectiveArgs.Add(_options.MemoryRoot);
        }

        return effectiveArgs.ToArray();
    }

    private static bool SupportsMemoryRoot(string commandName)
    {
        return string.Equals(commandName, "loop", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "respond", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "shell", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasOption(IEnumerable<string> args, string optionName)
    {
        var longOption = $"--{optionName}";
        return args.Any(arg => string.Equals(arg, longOption, StringComparison.OrdinalIgnoreCase));
    }

    private static void ResetMemory(ShellCommandOptions options)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var sessionRoot = string.IsNullOrWhiteSpace(options.MemoryRoot)
            ? Path.Combine(projectRoot, ".wallycode")
            : Path.GetFullPath(options.MemoryRoot);

        if (Directory.Exists(sessionRoot))
        {
            Directory.Delete(sessionRoot, recursive: true);
        }

        Console.WriteLine($"Reset session at {sessionRoot}");
        Console.WriteLine("A new session will be created the next time you run loop <goal>.");
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
