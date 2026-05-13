using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

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
        var (resolvedSourcePath, settings) = ResolveProjectContext();
        var effectiveMemoryRoot = string.IsNullOrWhiteSpace(_options.MemoryRoot)
            ? settings.RuntimeDefaults.MemoryRoot
            : _options.MemoryRoot;
        var sessionRoot = ProjectSettings.ResolveRuntimeRoot(resolvedSourcePath, effectiveMemoryRoot);

        PersistShellDefaults(settings, resolvedSourcePath, effectiveMemoryRoot);

        if (_options.ResetMemory)
        {
            ResetMemory(resolvedSourcePath, effectiveMemoryRoot);
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

        if (!string.IsNullOrWhiteSpace(effectiveMemoryRoot))
        {
            Console.WriteLine($"Shell initialized with memory root: {Path.GetFullPath(effectiveMemoryRoot)}");
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
                ResetMemory(resolvedSourcePath, effectiveMemoryRoot);
                Console.WriteLine();
                continue;
            }

            if (string.Equals(trimmed, "status", StringComparison.OrdinalIgnoreCase))
            {
                PrintStatus(resolvedSourcePath, sessionRoot);
                Console.WriteLine();
                continue;
            }

            var effectiveArgs = ApplyShellDefaults(args, resolvedSourcePath, effectiveMemoryRoot);
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

    private (string ProjectRoot, ProjectSettings Settings) ResolveProjectContext()
    {
        if (!string.IsNullOrWhiteSpace(_options.SourcePath))
        {
            var explicitRoot = ProjectSettings.ResolveProjectRoot(_options.SourcePath);
            return (explicitRoot, ProjectSettings.Load(explicitRoot));
        }

        if (_options.VsBuild)
        {
            var resolvedSourcePath = WorkspacePathResolver.ResolveVsBuildWorkspaceRoot(_appDirectoryPath);
            var projectRoot = ProjectSettings.ResolveProjectRoot(resolvedSourcePath);
            return (projectRoot, ProjectSettings.Load(projectRoot));
        }

        return ProjectSettings.ResolveProjectContext(null);
    }

    private void PersistShellDefaults(ProjectSettings settings, string projectRoot, string? effectiveMemoryRoot)
    {
        var changed = false;
        if (!string.IsNullOrWhiteSpace(_options.SourcePath) || _options.VsBuild)
        {
            if (!string.Equals(settings.RuntimeDefaults.SourcePath, projectRoot, StringComparison.Ordinal))
            {
                settings.RuntimeDefaults.SourcePath = projectRoot;
                changed = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.MemoryRoot))
        {
            var memoryRoot = Path.GetFullPath(_options.MemoryRoot);
            if (!string.Equals(settings.RuntimeDefaults.MemoryRoot, memoryRoot, StringComparison.Ordinal))
            {
                settings.RuntimeDefaults.MemoryRoot = memoryRoot;
                changed = true;
            }
        }

        if (changed)
        {
            settings.Save(projectRoot);
            _logger.LogAction("Saved shell defaults", $"source={settings.RuntimeDefaults.SourcePath}; memoryRoot={effectiveMemoryRoot}");
        }
    }

    private string[] ApplyShellDefaults(string[] args, string resolvedSourcePath, string? effectiveMemoryRoot)
    {
        var effectiveArgs = args.ToList();

        if (!HasOption(args, "source"))
        {
            effectiveArgs.Add("--source");
            effectiveArgs.Add(resolvedSourcePath);
        }

        if (SupportsMemoryRoot(args[0])
            && !HasOption(args, "memory-root")
            && !string.IsNullOrWhiteSpace(effectiveMemoryRoot))
        {
            effectiveArgs.Add("--memory-root");
            effectiveArgs.Add(effectiveMemoryRoot);
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
        return string.Equals(commandName, "run", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "step", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "ask", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "act", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "respond", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "recover", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "shell", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsLogging(string commandName)
    {
        return string.Equals(commandName, "run", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "step", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "ask", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "act", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "respond", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, "recover", StringComparison.OrdinalIgnoreCase);
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
        Console.WriteLine("A new session will be created the next time you run wallycode run <prompt> [workflow].");
    }

    private void PrintStatus(string resolvedSourcePath, string sessionRoot)
    {
        var settings = ProjectSettings.Load(resolvedSourcePath);
        Console.WriteLine($"Source:       {resolvedSourcePath}");
        Console.WriteLine($"Memory root:  {sessionRoot}");
        Console.WriteLine($"Provider:     {settings.Provider}");
        Console.WriteLine($"Model:        {settings.Model ?? "(provider default)"}");

        if (Session.Exists(sessionRoot))
        {
            try
            {
                var session = Session.Load(sessionRoot);
                Console.WriteLine($"Session:      [{session.Status}] {session.WorkflowName} → {session.ActiveStepName} (iteration {session.IterationCount})");
                Console.WriteLine($"Goal:         {session.Goal}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Session file exists but could not be read: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Session:      (none)");
        }
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
