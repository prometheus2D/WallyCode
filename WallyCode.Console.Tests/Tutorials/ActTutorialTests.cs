using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using WallyCode.ConsoleApp.Workflow;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class ActTutorialTests
{
    [Fact]
    public async Task Act_flow_moves_to_review_changes_and_keeps_implementation_context()
    {
        using var workspace = TutorialTestWorkspace.Create();
        var definition = WorkflowFixtures.ActDefinition();
        var provider = new TestLlmProvider()
            .RegisterResponse("{\"selectedStep\":\"to_review_changes\",\"summary\":\"implemented\",\"memory\":{\"implementation\":\"changed docs\"}}\n")
            .RegisterResponse("{\"selectedStep\":\"stop\",\"summary\":\"reviewed\",\"memory\":{\"review\":\"ok\"}}\n");

        workspace.CreateSession(definition, "Add a tutorial folder.");

        var orchestrator = new WorkflowOrchestrator(
            definition,
            workspace.RuntimeRoot,
            [new ProviderStepExecutor(provider)]);

        var first = await orchestrator.RunOnceAsync(CancellationToken.None);
        var second = await orchestrator.RunOnceAsync(CancellationToken.None);

        Assert.Equal("to_review_changes", first.SelectedStep);
        Assert.Equal("review_changes", first.ActiveStepName);
        Assert.Equal(SessionStatus.Active, first.Status);
        Assert.Equal("stop", second.SelectedStep);
        Assert.Equal(SessionStatus.Completed, second.Status);
        Assert.Equal(2, provider.Requests.Count);
        Assert.Contains("Add a tutorial folder.", provider.Requests[0].Prompt, StringComparison.Ordinal);

        var saved = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("changed docs", saved.Memory["implementation"]);
        Assert.Equal("ok", saved.Memory["review"]);
    }
}
