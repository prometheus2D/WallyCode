using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Commands;

[Collection("Console")]
public class TutorialCommandHandlerTests
{
    [Fact]
    public async Task List_writes_tutorial_names_and_summaries()
    {
        using var temp = TempWorkspace.Create();
        var tutorialsPath = CreateTutorials(temp.RootPath);
        var handler = new TutorialCommandHandler(new AppLogger(), tutorialsPath);

        var (exitCode, output) = await ExecuteAsync(handler, new TutorialCommandOptions { List = true });

        Assert.Equal(0, exitCode);
        Assert.Contains("Available tutorials:", output);
        Assert.Contains("book-story - Use this tutorial when you want WallyCode to help build a story as normal files in a repo.", output);
        Assert.Contains("repo-review - Use this tutorial when you want WallyCode to review a repository without changing files.", output);
        Assert.DoesNotContain("README", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShowTutorial_prints_tutorial_markdown()
    {
        using var temp = TempWorkspace.Create();
        var tutorialsPath = CreateTutorials(temp.RootPath);
        var handler = new TutorialCommandHandler(new AppLogger(), tutorialsPath);

        var (exitCode, output) = await ExecuteAsync(handler, new TutorialCommandOptions { Name = "repo-review" });

        Assert.Equal(0, exitCode);
        Assert.Contains("# repo-review", output);
        Assert.Contains("Use this tutorial when you want WallyCode to review a repository without changing files.", output);
    }

    [Fact]
    public async Task Missing_tutorial_returns_error_and_lists_available_items()
    {
        using var temp = TempWorkspace.Create();
        var tutorialsPath = CreateTutorials(temp.RootPath);
        var handler = new TutorialCommandHandler(new AppLogger(), tutorialsPath);

        var (exitCode, output) = await ExecuteAsync(handler, new TutorialCommandOptions { Name = "missing" });

        Assert.Equal(1, exitCode);
        Assert.Contains("Tutorial 'missing' was not found.", output);
        Assert.Contains("Available tutorials:", output);
        Assert.Contains("book-story - Use this tutorial when you want WallyCode to help build a story as normal files in a repo.", output);
    }

    private static async Task<(int ExitCode, string Output)> ExecuteAsync(
        TutorialCommandHandler handler,
        TutorialCommandOptions options)
    {
        var writer = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(writer);
            var exitCode = await handler.ExecuteAsync(options, CancellationToken.None);
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string CreateTutorials(string rootPath)
    {
        var tutorialsPath = Path.Combine(rootPath, "Tutorials");
        Directory.CreateDirectory(tutorialsPath);

        File.WriteAllText(Path.Combine(tutorialsPath, "README.md"), "# WallyCode Tutorials\n");
        File.WriteAllText(Path.Combine(tutorialsPath, "book-story.md"), "# book-story\n\nUse this tutorial when you want WallyCode to help build a story as normal files in a repo.\n\n## Best fit\n");
        File.WriteAllText(Path.Combine(tutorialsPath, "repo-review.md"), "# repo-review\n\nUse this tutorial when you want WallyCode to review a repository without changing files.\n\n## Best fit\n");

        return tutorialsPath;
    }
}