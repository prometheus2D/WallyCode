using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ShellCommandHandler
{
    private readonly ShellCommandOptions _options;
    private readonly string _appDirectoryPath;
    private readonly AppLogger _logger = new();

    public ShellCommandHandler(ShellCommandOptions options, string? appDirectoryPath = null)
    {
        _options = options;
        _appDirectoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppContext.BaseDirectory
            : appDirectoryPath);
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var resolvedSourcePath = ResolveSourcePath();
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(resolvedSourcePath, _options.MemoryRoot);

        if (_options.ResetMemory)
        {
            ResetMemory(resolvedSourcePath, _options.MemoryRoot);
        }

        ConfigureShellLogging(sessionRoot);
        _logger.LogAction("Shell initialized", $"source={resolvedSourcePath}; sessionRoot={sessionRoot}; vsBuild={_options.VsBuild}");

        Console.WriteLine("WallyCode shell");

        if (_options.VsBuild)
        {
            Console.WriteLine("VS build mode enabled.");
            Console.WriteLine($"\tDirectory before VS resolution: {_appDirectoryPath}");
            Console.WriteLine($"\tDirectory after VS resolution:  {resolvedSourcePath}");
        }
        else
        {
            Console.WriteLine($"Shell initialized with source: {resolvedSourcePath}");
        }

        if (!string.IsNullOrWhiteSpace(_options.MemoryRoot))
        {
            Console.WriteLine($"Shell initialized with memory root: {Path.GetFullPath(_options.MemoryRoot)}");
        }
        else
        {
            Console.WriteLine($"Shell using default memory root: {ProjectSettings.ResolveRuntimeRoot(resolvedSourcePath, memoryRoot: null)}");
        }

        if (_options.Log)
        {
            Console.WriteLine($"Shell logging enabled{(_options.Verbose ? " (verbose)" : string.Empty)}.");
        }

        Console.WriteLine("Type a WallyCode command without the executable name.");
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("wallycode> ");
            var input = Console.ReadLine();

            if (input is null)
            {
                _logger.LogAction("Shell exit", "Console input closed.");
                return 0;
            }

            var trimmed = input.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (string.Equals(trimmed, "exit", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogAction("Shell exit", "User entered exit.");
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
                _logger.LogAction("Shell command skipped", "Nested shell command ignored.");
                continue;
            }

            if (string.Equals(args[0], "reset-memory", StringComparison.OrdinalIgnoreCase))
            {
                ResetMemory(resolvedSourcePath, _options.MemoryRoot);
                Console.WriteLine();
                continue;
            }

            var effectiveArgs = ApplyShellDefaults(args, resolvedSourcePath);
            _logger.LogAction("Executing shell subcommand", string.Join(" ", effectiveArgs), verboseOnly: true);
            await Program.RunAsync(effectiveArgs, cancellationToken, _appDirectoryPath);
            Console.WriteLine();
        }

        _logger.LogAction("Shell exit", "Cancellation requested.");
        return 0;
    }

    private void ConfigureShellLogging(string sessionRoot)
    {
        _logger.ConfigureLogging(sessionRoot, new LoggingMode
        {
            Enabled = _options.Log,
            Verbose = _options.Verbose
        });
    }

    private string ResolveSourcePath()
    {
        var sourcePath = !string.IsNullOrWhiteSpace(_options.SourcePath)
            ? _options.SourcePath
            : _options.VsBuild
                ? WorkspacePathResolver.ResolveVsBuildWorkspaceRoot(_appDirectoryPath)
                : null;

        return ProjectSettings.ResolveProjectRoot(sourcePath);
    }

    private string[] ApplyShellDefaults(string[] args, string resolvedSourcePath)
    {
        var effectiveArgs = args.ToList();

        if (!HasOption(args, "source"))
        {
            effectiveArgs.Add("--source");
            effectiveArgs.Add(resolvedSourcePath);
        }

        if (SupportsMemoryRoot(args[0])
            && !HasOption(args, "memory-root")
            && !string.IsNullOrWhiteSpace(_options.MemoryRoot))
        {
            effectiveArgs.Add("--memory-root");
            effectiveArgs.Add(_options.MemoryRoot);
        }

        if (SupportsLogging(args[0]) && _options.Log && !HasOption(args, "log"))
        {
            effectiveArgs.Add("--log");
        }

        if (SupportsLogging(args[0]) && _options.Verbose && !HasOption(args, "verbose"))
        {
            effectiveArgs.Add("--verbose");
        }

        _logger.LogAction("Applied shell defaults", string.Join(" ", effectiveArgs), verboseOnly: true);
        return effectiveArgs.ToArray();
    }

    private static bool SupportsMemoryRoot(string commandName)
    {
        return string.Equals(commandName, "loop", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "ask", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "act", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "respond", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "shell", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsLogging(string commandName)
    {
        return string.Equals(commandName, "loop", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "ask", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "act", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "respond", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasOption(IEnumerable<string> args, string optionName)
    {
        var longOption = $"--{optionName}";
        return args.Any(arg => string.Equals(arg, longOption, StringComparison.OrdinalIgnoreCase));
    }

    private void ResetMemory(string resolvedSourcePath, string? memoryRoot)
    {
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(resolvedSourcePath, memoryRoot);

        if (Directory.Exists(sessionRoot))
        {
            Directory.Delete(sessionRoot, recursive: true);
        }

        _logger.LogAction("Reset memory", $"sessionRoot={sessionRoot}");
        Console.WriteLine($"Reset session state at {sessionRoot}");
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
