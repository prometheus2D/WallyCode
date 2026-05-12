using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class DevelopmentModeTutorialTests
{
    [Fact]
    public void Isolated_runtime_root_can_be_resolved_for_local_source_build_workflows()
    {
        using var workspace = TutorialTestWorkspace.Create();
        var settings = new ProjectSettings();
        settings.RuntimeDefaults.SourcePath = workspace.ProjectRoot;
        settings.RuntimeDefaults.MemoryRoot = Path.Combine(workspace.ProjectRoot, ".wallycode-dev");
        settings.Save(workspace.ProjectRoot);

        var loaded = ProjectSettings.Load(workspace.ProjectRoot);
        var runtimeRoot = ProjectSettings.ResolveSessionRoot(loaded, workspace.ProjectRoot, null);

        Assert.Equal(Path.Combine(workspace.ProjectRoot, ".wallycode-dev"), runtimeRoot);
        Assert.Equal(workspace.ProjectRoot, loaded.RuntimeDefaults.SourcePath);
    }
}
