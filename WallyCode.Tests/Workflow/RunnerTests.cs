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
            new MockInvocation { RawOutput = """{"selectedStep":"continue","summary":"working"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("continue", result.SelectedStep);
        Assert.Equal("collect_requirements", result.ActiveStepName);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.False(result.StopsInvocation);
        Assert.Equal("working", result.Summary);

        var session = Session.Load(temp.RootPath);
        Assert.Equal(1, session.IterationCount);
        Assert.Equal("continue", session.LastSelectedStep);
    }

    [Fact]
    public async Task Selected_next_step_moves_to_target_step()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("full-pipeline");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedStep":"produce_tasks"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("produce_tasks", result.ActiveStepName);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.False(result.StopsInvocation);
    }

    [Theory]
    [InlineData("ask_user", SessionStatus.Blocked)]
    [InlineData("stop", SessionStatus.Completed)]
    [InlineData("done", SessionStatus.Completed)]
    [InlineData("error", SessionStatus.Error)]
    public async Task Terminal_selections_update_status_and_stop(string selectedStep, string expectedStatus)
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("full-pipeline");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        if (selectedStep is "stop" or "done" or "error")
        {
            session.ActiveStepName = "execute_tasks";
        }
        session.Save(temp.RootPath);

        var runner = new Runner(
            new MockLlmProvider([new MockInvocation { RawOutput = $$"""{"selectedStep":"{{selectedStep}}","summary":"problem details"}""" }]),
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
    public async Task Invalid_selected_step_throws(string selectedStep)
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("full-pipeline");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.ActiveStepName = "produce_tasks";
        session.Save(temp.RootPath);

        var runner = new Runner(
            new MockLlmProvider([new MockInvocation { RawOutput = $$"""{"selectedStep":"{{selectedStep}}"}""" }]),
            def, temp.RootPath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));

        var persisted = Session.Load(temp.RootPath);
        Assert.Equal(SessionStatus.Error, persisted.Status);
        Assert.Equal("error", persisted.LastSelectedStep);
        Assert.False(string.IsNullOrWhiteSpace(persisted.LastSummary));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task Malformed_or_missing_selected_step_json_throws(string rawOutput)
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = rawOutput });

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));

        var persisted = Session.Load(temp.RootPath);
        Assert.Equal(SessionStatus.Error, persisted.Status);
        Assert.Equal("error", persisted.LastSelectedStep);
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
        Assert.Equal("error", persisted.LastSelectedStep);
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
            new MockInvocation { RawOutput = "```json\n{\"selectedStep\":\"continue\"}\n```" });

        var result = await runner.RunOnceAsync(CancellationToken.None);
        Assert.Equal("continue", result.SelectedStep);
    }

    [Fact]
    public async Task Run_steps_stops_on_blocking_selection()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.Save(temp.RootPath);
        var runner = new Runner(
            new MockLlmProvider([
                new MockInvocation { RawOutput = """{"selectedStep":"continue"}""" },
                new MockInvocation { RawOutput = """{"selectedStep":"ask_user"}""" },
                new MockInvocation { RawOutput = """{"selectedStep":"continue"}""" }
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
            new MockInvocation { RawOutput = """{"selectedStep":"continue"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("user said csv", provider.Requests[0].Prompt);
        Assert.Empty(Session.Load(temp.RootPath).PendingResponses);
    }

    [Fact]
    public async Task Prompt_includes_step_transitions()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedStep":"continue"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Step transitions:", provider.Requests[0].Prompt);
        Assert.Contains("continue:", provider.Requests[0].Prompt);
        Assert.Contains("produce_tasks:", provider.Requests[0].Prompt);
        Assert.DoesNotContain("stop:", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Memory_updates_are_persisted_injected_and_snapshotted()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("full-pipeline");
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedStep":"produce_tasks","summary":"requirements ready","memory":{"requirements":"Import comma-separated files."}}"""
            },
            new MockInvocation
            {
                RawOutput = """{"selectedStep":"continue","summary":"task draft","memory":{"tasks":["Parse CSV","Validate rows"]}}"""
            }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);

        var results = await runner.RunAsync(steps: 2, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains("Session memory:", provider.Requests[1].Prompt);
        Assert.Contains("requirements: Import comma-separated files.", provider.Requests[1].Prompt);
        Assert.Contains("Memory this step can update:", provider.Requests[1].Prompt);
        Assert.Contains("tasks", provider.Requests[1].Prompt);

        var session = Session.Load(temp.RootPath);
        Assert.Equal("Import comma-separated files.", session.Memory["requirements"]);
        Assert.Equal("[\"Parse CSV\",\"Validate rows\"]", session.Memory["tasks"]);
        Assert.True(File.Exists(Session.SnapshotFilePath(temp.RootPath, 1)));
        Assert.True(File.Exists(Session.SnapshotFilePath(temp.RootPath, 2)));
    }

    [Fact]
    public async Task Memory_null_update_removes_existing_key()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.Memory["requirements"] = "old";
        session.Save(temp.RootPath);

        var runner = new Runner(
            new MockLlmProvider([new MockInvocation { RawOutput = """{"selectedStep":"continue","memory":{"requirements":null}}""" }]),
            def,
            temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.False(Session.Load(temp.RootPath).Memory.ContainsKey("requirements"));
    }

    [Fact]
    public async Task Prompt_includes_terminal_error_outcome_guidance()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedStep":"continue"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Terminal outcomes:", provider.Requests[0].Prompt);
        Assert.Contains("These outcomes stop this invocation and do not target workflow steps.", provider.Requests[0].Prompt);
        Assert.Contains("error:", provider.Requests[0].Prompt);
        Assert.Contains("Put the user-visible reason in summary", provider.Requests[0].Prompt);
        Assert.DoesNotContain("[FAIL]", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_does_not_include_global_prompt_when_not_configured()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedStep":"continue"}""" }
        ]);
        var runner = new Runner(provider, def, temp.RootPath);
        Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.DoesNotContain("Global prompt:", provider.Requests[0].Prompt);
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
