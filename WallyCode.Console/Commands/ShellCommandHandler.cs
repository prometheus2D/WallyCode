using CommandLine;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ShellCommandHandler
{
    public async Task<int> ExecuteAsync(string[] originalArgs, CancellationToken cancellationToken)
    {
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

            await Program.RunAsync(args, cancellationToken);
            Console.WriteLine();
        }

        return 0;
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
