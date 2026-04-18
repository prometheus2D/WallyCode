using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class TutorialCommandHandler
{
    private readonly AppLogger _logger;
    private readonly string _tutorialsPath;

    public TutorialCommandHandler(AppLogger logger, string? tutorialsPath = null)
    {
        _logger = logger;
        _tutorialsPath = string.IsNullOrWhiteSpace(tutorialsPath)
            ? TutorialCatalog.GetDefaultPath()
            : Path.GetFullPath(tutorialsPath);
    }

    public Task<int> ExecuteAsync(TutorialCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tutorials = TutorialCatalog.Load(_tutorialsPath);

        _logger.Section("WallyCode Tutorial");

        if (options.List)
        {
            WriteTutorialList(tutorials);
            return Task.FromResult(0);
        }

        if (!string.IsNullOrWhiteSpace(options.Name))
        {
            return Task.FromResult(ShowTutorial(tutorials, options.Name.Trim()));
        }

        WriteOverview(tutorials);
        return Task.FromResult(0);
    }

    private void WriteOverview(IReadOnlyList<TutorialDocument> tutorials)
    {
        _logger.Info("Tutorials are markdown guides you can list and open from the CLI.");
        Console.WriteLine($"Tutorial folder: {_tutorialsPath}");
        Console.WriteLine();
        Console.WriteLine("Recommended path:");
        Console.WriteLine("1. Start with ask for direct answers.");
        Console.WriteLine("2. Move to act when you want direct repo changes.");
        Console.WriteLine("3. Use loop when the task needs iteration and memory.");
        Console.WriteLine("4. Use provider commands once you know your preferred model setup.");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("tutorial --list");

        foreach (var tutorial in tutorials)
        {
            Console.WriteLine($"tutorial {tutorial.Name}");
        }

        Console.WriteLine();
        WriteTutorialList(tutorials);
    }

    private void WriteTutorialList(IReadOnlyList<TutorialDocument> tutorials)
    {
        Console.WriteLine("Available tutorials:");
        Console.WriteLine();

        if (tutorials.Count == 0)
        {
            Console.WriteLine("  (none found)");
            Console.WriteLine();
            Console.WriteLine($"Looked in: {_tutorialsPath}");
            Console.WriteLine();
            return;
        }

        foreach (var tutorial in tutorials)
        {
            Console.WriteLine($"  {tutorial.Name} - {tutorial.Summary}");
        }

        Console.WriteLine();
        Console.WriteLine("Use 'tutorial <name>' to display one.");
        Console.WriteLine();
    }

    private int ShowTutorial(IReadOnlyList<TutorialDocument> tutorials, string name)
    {
        var tutorial = tutorials.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

        if (tutorial is null)
        {
            _logger.Error($"Tutorial '{name}' was not found. Use 'tutorial --list' to see available tutorials.");
            Console.WriteLine();
            WriteTutorialList(tutorials);
            return 1;
        }

        Console.WriteLine(tutorial.Content.TrimEnd());
        Console.WriteLine();
        return 0;
    }
}
