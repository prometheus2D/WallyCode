using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class SetupTutorialTests
{
    [Fact]
    public async Task Setup_command_creates_wallycode_json_and_runtime_folder()
    {
        using var workspace = TutorialTestWorkspace.Create();
        var provider = new TestLlmProvider
        {
            Name = "gh-copilot-claude",
            DefaultModel = "claude-sonnet-4"
        };

        var registry = new ProviderRegistry([provider]);
        var handler = new SetupCommandHandler(registry, new AppLogger(), workspace.ProjectRoot);

        var exitCode = await handler.ExecuteAsync(new SetupCommandOptions { DirectoryPath = workspace.ProjectRoot }, CancellationToken.None);

        Assert.Equal(0, exitCode);

        var filePath = Path.Combine(workspace.ProjectRoot, "wallycode.json");
        Assert.True(File.Exists(filePath));
        Assert.True(Directory.Exists(workspace.RuntimeRoot));

        var loaded = ProjectSettings.Load(workspace.ProjectRoot);
        Assert.Equal(workspace.ProjectRoot, loaded.RuntimeDefaults.SourcePath);
        Assert.Equal("gh-copilot-claude", loaded.Provider);
        Assert.Equal("claude-sonnet-4", loaded.Model);
    }
}
