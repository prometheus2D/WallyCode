using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Workflow;

public class RunnerTests
{
    private static Runner NewRunner(string root, WorkflowDefinition def, params MockInvocation[] script)
    {
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", root);
        session.Save(root);
        return new Runner(new MockLlmProvider(script), def, root);
    }

    [Fact]
    public async Task Continue_keeps_active_step_and_increments_iteration()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"working"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("[CONTINUE]", result.SelectedKeyword);
        Assert.Equal("collect_requirements", result.ActiveStepName);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.False(result.StopsInvocation);
        Assert.Equal("working", result.Summary);

        var session = Session.Load(temp.RootPath);
        Assert.Equal(1, session.IterationCount);
        Assert.Equal("[CONTINUE]", session.LastSelectedKeyword);
    }

    [Fact]
    public async Task Transition_keyword_moves_to_target_step()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[REQUIREMENTS_READY]"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("produce_tasks", result.ActiveStepName);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.False(result.StopsInvocation);
    }

    [Theory]
    [InlineData("[ASK_USER]", SessionStatus.Blocked)]
    [InlineData("[DONE]", SessionStatus.Completed)]
    [InlineData("[ERROR]", SessionStatus.Error)]
    public async Task Terminal_keywords_update_status_and_stop(string keyword, string expectedStatus)
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        if (keyword is "[DONE]" or "[ERROR]")
        {
            session.ActiveStepName = "produce_tasks";
        }
        session.Save(temp.RootPath);

        var runner = new Runner(
            new MockLlmProvider([new MockInvocation { RawOutput = $$"""{"selectedKeyword":"{{keyword}}","summary":"problem details"}""" }]),
            def, temp.RootPath);

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.StopsInvocation);
        Assert.Equal("problem details", Session.Load(temp.RootPath).LastSummary);
    }

    [Theory]
    [InlineData("DONE")]
    [InlineData("[BOGUS]")]
    [InlineData("[FAIL]")]
    public async Task Invalid_keyword_throws(string keyword)
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.ActiveStepName = "produce_tasks";
        session.Save(temp.RootPath);

        var runner = new Runner(
            new MockLlmProvider([new MockInvocation { RawOutput = $$"""{"selectedKeyword":"{{keyword}}"}""" }]),
            def, temp.RootPath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));

        var persisted = Session.Load(temp.RootPath);
        Assert.Equal(SessionStatus.Error, persisted.Status);
        Assert.Equal("[ERROR]", persisted.LastSelectedKeyword);
        Assert.False(string.IsNullOrWhiteSpace(persisted.LastSummary));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task Malformed_or_missing_keyword_json_throws(string rawOutput)
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = rawOutput });

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));

        var persisted = Session.Load(temp.RootPath);
        Assert.Equal(SessionStatus.Error, persisted.Status);
        Assert.Equal("[ERROR]", persisted.LastSelectedKeyword);
    }

    [Fact]
    public async Task Provider_failure_parks_session_in_error_state()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation { Exception = new InvalidOperationException("provider unreachable") }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
        Assert.Equal("provider unreachable", ex.Message);

        var persisted = Session.Load(temp.RootPath);
        Assert.Equal(SessionStatus.Error, persisted.Status);
        Assert.Equal("[ERROR]", persisted.LastSelectedKeyword);
        Assert.Equal("provider unreachable", persisted.LastSummary);
        Assert.Equal(1, persisted.IterationCount);
        Assert.Empty(persisted.PendingResponses);
    }

    [Fact]
    public async Task Code_fenced_json_is_parsed()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = "```json\n{\"selectedKeyword\":\"[CONTINUE]\"}\n```" });

        var result = await runner.RunOnceAsync(CancellationToken.None);
        Assert.Equal("[CONTINUE]", result.SelectedKeyword);
    }

    [Fact]
    public async Task Run_steps_stops_on_blocking_keyword()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.Save(temp.RootPath);
        var runner = new Runner(
            new MockLlmProvider([
                new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" },
                new MockInvocation { RawOutput = """{"selectedKeyword":"[ASK_USER]"}""" },
                new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
            ]),
            def, temp.RootPath);

        var results = await runner.RunAsync(steps: 5, CancellationToken.None);
        Assert.Equal(2, results.Count);
        Assert.Equal(SessionStatus.Blocked, results[^1].Status);
    }

    [Fact]
    public async Task Pending_responses_appear_in_prompt_then_clear()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.PendingResponses.Add("user said csv");
        session.Save(temp.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("user said csv", provider.Requests[0].Prompt);
        Assert.Empty(Session.Load(temp.RootPath).PendingResponses);
    }

    [Fact]
    public async Task Prompt_includes_keyword_descriptions()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Keyword options:", provider.Requests[0].Prompt);
        Assert.Contains("[REQUIREMENTS_READY]:", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_mentions_error_keyword_guidance()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("select [ERROR]", provider.Requests[0].Prompt);
        Assert.DoesNotContain("[FAIL]", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_does_not_include_global_prompt_when_not_configured()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.DoesNotContain("Global prompt:", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_uses_workspace_global_prompt_when_present()
    {
        using var temp = TempWorkspace.Create();
        var settings = new ProjectSettings
        {
            GlobalPrompt = "Always preserve brackets in keywords."
        };
        settings.Save(temp.RootPath);

        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Global prompt:", provider.Requests[0].Prompt);
        Assert.Contains("Always preserve brackets in keywords.", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Run_throws_when_session_workflow_does_not_match()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.WorkflowName = "other";
        session.Save(temp.RootPath);

        var runner = new Runner(new MockLlmProvider([]), def, temp.RootPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }
}
