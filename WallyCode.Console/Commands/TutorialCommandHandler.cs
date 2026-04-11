using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class TutorialCommandHandler
{
    private readonly AppLogger _logger;

    public TutorialCommandHandler(AppLogger logger)
    {
        _logger = logger;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.Section("WallyCode Tutorial");
        _logger.Info("WallyCode has two main modes: prompt for one-shot work, and loop for iterative work with memory.");
        Console.WriteLine();

        WriteStep("1. Start with a one-shot prompt", [
            "prompt \"Summarize this repository in one short paragraph.\"",
            "prompt \"Create a simple browser-based tic-tac-toe game in this repo.\""
        ]);

        WriteStep("2. Point WallyCode at a different repo with --source", [
            "prompt \"Summarize this repository in one short paragraph.\" --source C:\\src\\my-repo"
        ]);

        WriteStep("3. Use loop when you want state across iterations", [
            "loop \"Build a simple browser-based tic-tac-toe game in this repo. Do one small bounded chunk per iteration and stop when complete.\"",
            "loop",
            "respond \"Keep the UI minimal and readable.\""
        ]);

        WriteStep("4. Use --memory-root when you want loop state somewhere else", [
            "loop \"Work on issue 123\" --source C:\\src\\my-repo --memory-root C:\\temp\\wallycode-session"
        ]);

        WriteStep("5. Use shell when you want to stay in an interactive session", [
            "shell --source C:\\src\\my-repo --memory-root C:\\temp\\wallycode-session",
            "prompt \"Summarize this repository\"",
            "loop \"Work on issue 123\"",
            "loop",
            "exit"
        ]);

        WriteStep("6. Providers are secondary configuration", [
            "provider",
            "provider --models",
            "provider gh-copilot-gpt5 --set",
            "provider --model gpt-5"
        ]);

        Console.WriteLine("Mental model:");
        Console.WriteLine("- source = where the provider operates and where files can be changed");
        Console.WriteLine("- memory-root = where WallyCode stores loop memory, prompts, raw output, logs, and session state");
        Console.WriteLine("- prompt = one-shot");
        Console.WriteLine("- loop = iterative work with memory");
        Console.WriteLine("- shell = interactive wrapper around the same commands");
        Console.WriteLine();
        Console.WriteLine("Recommended path:");
        Console.WriteLine("1. Try prompt.");
        Console.WriteLine("2. Move to loop when the task needs iteration.");
        Console.WriteLine("3. Use shell when you want repeated commands in one session.");

        return Task.FromResult(0);
    }

    private static void WriteStep(string title, IReadOnlyList<string> commands)
    {
        Console.WriteLine(title);
        Console.WriteLine();

        foreach (var command in commands)
        {
            Console.WriteLine(command);
        }

        Console.WriteLine();
    }
}
