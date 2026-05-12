using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using WallyCode.ConsoleApp.Workflow;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class StepwiseTutorialTests
{
    [Fact]
    public async Task Requirements_flow_persists_snapshots_across_iterations()
    {
        using var workspace = TutorialTestWorkspace.Create();
        var definition = WorkflowFixtures.RequirementsDefinition();
        var provider = new TestLlmProvider()
            .RegisterResponse("{\"selectedStep\":\"to_produce_tasks\",\"summary\":\"requirements captured\",\"memory\":{\"requirements\":\"done\"}}\n")
            .RegisterResponse("{\"selectedStep\":\"to_execute_tasks\",\"summary\":\"tasks produced\",\"memory\":{\"tasks\":\"1,2,3\"}}\n")
            .RegisterResponse("{\"selectedStep\":\"stop\",\"summary\":\"execution complete\",\"memory\":{\"execution\":\"finished\"}}\n");

        workspace.CreateSession(definition, "Build a CSV importer.");

        var orchestrator = new WorkflowOrchestrator(
            definition,
            workspace.RuntimeRoot,
            [new ProviderStepExecutor(provider)]);

        var results = await orchestrator.RunAsync(3, CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.True(File.Exists(Session.FilePath(workspace.RuntimeRoot)));
        Assert.True(File.Exists(Session.SnapshotFilePath(workspace.RuntimeRoot, 1)));
        Assert.True(File.Exists(Session.SnapshotFilePath(workspace.RuntimeRoot, 2)));
        Assert.True(File.Exists(Session.SnapshotFilePath(workspace.RuntimeRoot, 3)));

        var saved = Session.Load(workspace.RuntimeRoot);
        Assert.Equal(SessionStatus.Completed, saved.Status);
        Assert.Equal(3, saved.IterationCount);
        Assert.Equal("finished", saved.Memory["execution"]);
    }
}
