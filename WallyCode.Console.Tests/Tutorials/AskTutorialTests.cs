using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using WallyCode.ConsoleApp.Workflow;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class AskTutorialTests
{
    [Fact]
    public async Task Ask_flow_persists_session_and_memory()
    {
        using var workspace = TutorialTestWorkspace.Create();
        var definition = WorkflowFixtures.AskDefinition();
        var provider = new TestLlmProvider().RegisterResponse("{\"selectedStep\":\"stop\",\"summary\":\"done\",\"memory\":{\"answer\":\"42\"}}\n");
        var session = workspace.CreateSession(definition, "What does this repository do?");

        var orchestrator = new WorkflowOrchestrator(
            definition,
            workspace.RuntimeRoot,
            [new ProviderStepExecutor(provider)]);

        var results = await orchestrator.RunAsync(1, CancellationToken.None);

        Assert.Single(results);
        Assert.Single(provider.Requests);
        Assert.Contains("What does this repository do?", provider.Requests[0].Prompt);
        Assert.Equal("done", results[0].Summary);
        Assert.Equal(SessionStatus.Completed, results[0].Status);
        Assert.True(File.Exists(Session.FilePath(workspace.RuntimeRoot)));

        var saved = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("42", saved.Memory["answer"]);
        Assert.Equal(1, saved.IterationCount);
        Assert.Equal(SessionStatus.Completed, saved.Status);
        Assert.Equal("stop", saved.LastSelectedStep);
    }
}
