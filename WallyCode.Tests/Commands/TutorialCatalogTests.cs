using WallyCode.ConsoleApp.Commands;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Commands;

public class TutorialCatalogTests
{
    [Fact]
    public void Load_excludes_readme_and_extracts_summary()
    {
        using var temp = TempWorkspace.Create();
        var tutorialsPath = Path.Combine(temp.RootPath, "Tutorials");
        Directory.CreateDirectory(tutorialsPath);

        File.WriteAllText(Path.Combine(tutorialsPath, "README.md"), "# WallyCode Tutorials\n");
        File.WriteAllText(Path.Combine(tutorialsPath, "book-story.md"), "# book-story\n\nUse this tutorial when you want WallyCode to help build a story as normal files in a repo.\n\n## Best fit\n");
        File.WriteAllText(Path.Combine(tutorialsPath, "repo-review.md"), "# repo-review\n\nUse this tutorial when you want WallyCode to review a repository without changing files.\n");

        var tutorials = TutorialCatalog.Load(tutorialsPath);

        Assert.Collection(
            tutorials,
            bookStory =>
            {
                Assert.Equal("book-story", bookStory.Name);
                Assert.Equal("book-story", bookStory.Title);
                Assert.Equal("Use this tutorial when you want WallyCode to help build a story as normal files in a repo.", bookStory.Summary);
            },
            repoReview =>
            {
                Assert.Equal("repo-review", repoReview.Name);
                Assert.Equal("repo-review", repoReview.Title);
                Assert.Equal("Use this tutorial when you want WallyCode to review a repository without changing files.", repoReview.Summary);
            });
    }

    [Fact]
    public void Load_returns_empty_when_directory_does_not_exist()
    {
        using var temp = TempWorkspace.Create();

        var tutorials = TutorialCatalog.Load(Path.Combine(temp.RootPath, "missing"));

        Assert.Empty(tutorials);
    }
}