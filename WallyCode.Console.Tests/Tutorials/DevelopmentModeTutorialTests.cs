using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class DevelopmentModeTutorialTests
{
    [Fact]
    public void Isolated_runtime_root_can_be_resolved_for_local_source_build_workflows()
    {
        using var workspace = TutorialTestWorkspace.Create(runSetup: true);
        var settings = ProjectSettings.Load(workspace.ProjectRoot);
        var customMemoryRoot = Path.Combine(workspace.ProjectRoot, ".wallycode-dev");
        Directory.CreateDirectory(customMemoryRoot);
        
        var runtimeRoot = ProjectSettings.ResolveSessionRoot(settings, workspace.ProjectRoot, customMemoryRoot);

        Assert.Equal(customMemoryRoot, runtimeRoot);
        Assert.Equal(workspace.ProjectRoot, settings.RuntimeDefaults.SourcePath);
    }
}
