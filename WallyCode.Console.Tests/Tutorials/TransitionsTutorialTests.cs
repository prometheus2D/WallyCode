using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using WallyCode.ConsoleApp.Workflow;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class TransitionsTutorialTests
{
    [Fact]
    public async Task Handoff_memory_routes_to_next_step()
    {
        using var workspace = TutorialTestWorkspace.Create();
        var definition = WorkflowFixtures.HandoffDefinition();
        var provider = new TestLlmProvider()
            .RegisterResponse("{\"selectedStep\":\"to_second\",\"summary\":\"handoff ready\",\"memory\":{\"handoff\":\"value\"}}\n")
            .RegisterResponse("{\"selectedStep\":\"stop\",\"summary\":\"done\"}\n");

        workspace.CreateSession(definition, "Verify transition handoff.");

        var orchestrator = new WorkflowOrchestrator(
            definition,
            workspace.RuntimeRoot,
            [new ProviderStepExecutor(provider)]);

        var first = await orchestrator.RunOnceAsync(CancellationToken.None);
        var second = await orchestrator.RunOnceAsync(CancellationToken.None);

        Assert.Equal("to_second", first.SelectedStep);
        Assert.Equal("second", first.ActiveStepName);
        Assert.Equal(SessionStatus.Active, first.Status);
        Assert.Equal("stop", second.SelectedStep);
        Assert.Equal(SessionStatus.Completed, second.Status);

        var saved = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("value", saved.Memory["handoff"]);
    }
}