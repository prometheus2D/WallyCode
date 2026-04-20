using WallyCode.ConsoleApp.Routing;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Routing;

public class RoutedRunnerTests
{
    private static RoutedRunner NewRunner(string root, RoutingDefinition def, params MockInvocation[] script)
    {
        var session = TestDefinitions.NewSession(def, root);
        session.Save(root);
        return new RoutedRunner(new MockLlmProvider(script), def, root);
    }

    [Fact]
    public async Task Continue_keeps_active_unit_and_increments_iteration()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"working"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("[CONTINUE]", result.SelectedKeyword);
        Assert.Equal("start", result.ActiveUnitName);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.False(result.StopsInvocation);
        Assert.Equal("working", result.Summary);

        var session = RoutedSession.Load(temp.RootPath);
        Assert.Equal(1, session.IterationCount);
        Assert.Equal("[CONTINUE]", session.LastSelectedKeyword);
    }

    [Fact]
    public async Task Transition_keyword_moves_to_target_unit()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[NEXT]"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("finish", result.ActiveUnitName);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.False(result.StopsInvocation);
    }

    [Fact]
    public async Task AskUser_blocks_session_and_stops_invocation()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[ASK_USER]"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal(SessionStatus.Blocked, result.Status);
        Assert.True(result.StopsInvocation);
    }

    [Fact]
    public async Task Done_completes_session()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var session = TestDefinitions.NewSession(def, temp.RootPath);
        session.ActiveUnitName = "finish";
        session.Save(temp.RootPath);

        var runner = new RoutedRunner(
            new MockLlmProvider([new MockInvocation { RawOutput = """{"selectedKeyword":"[DONE]"}""" }]),
            def, temp.RootPath);

        var result = await runner.RunOnceAsync(CancellationToken.None);
        Assert.Equal(SessionStatus.Completed, result.Status);
        Assert.True(result.StopsInvocation);
    }

    [Fact]
    public async Task Fail_marks_session_failed()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[FAIL]"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);
        Assert.Equal(SessionStatus.Failed, result.Status);
        Assert.True(result.StopsInvocation);
    }

    [Fact]
    public async Task Keyword_not_in_allowed_throws()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[BOGUS]"}""" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Malformed_json_throws()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = "not json" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Code_fenced_json_is_parsed()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = "```json\n{\"selectedKeyword\":\"[CONTINUE]\"}\n```" });

        var result = await runner.RunOnceAsync(CancellationToken.None);
        Assert.Equal("[CONTINUE]", result.SelectedKeyword);
    }

    [Fact]
    public async Task Run_steps_stops_on_blocking_keyword()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var session = TestDefinitions.NewSession(def, temp.RootPath);
        session.Save(temp.RootPath);
        var runner = new RoutedRunner(
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
        var def = TestDefinitions.TwoUnit();
        var session = TestDefinitions.NewSession(def, temp.RootPath);
        session.PendingResponses.Add("user said csv");
        session.Save(temp.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new RoutedRunner(provider, def, temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("user said csv", provider.Requests[0].Prompt);
        Assert.Empty(RoutedSession.Load(temp.RootPath).PendingResponses);
    }

    [Fact]
    public async Task Prompt_includes_keyword_descriptions()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new RoutedRunner(provider, def, temp.RootPath);
        TestDefinitions.NewSession(def, temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Keyword options:", provider.Requests[0].Prompt);
        Assert.Contains("[NEXT]: Move to the finish unit.", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Run_throws_when_session_definition_does_not_match()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var session = TestDefinitions.NewSession(def, temp.RootPath);
        session.DefinitionName = "other";
        session.Save(temp.RootPath);

        var runner = new RoutedRunner(new MockLlmProvider([]), def, temp.RootPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Run_throws_when_session_completed()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var session = TestDefinitions.NewSession(def, temp.RootPath);
        session.Status = SessionStatus.Completed;
        session.Save(temp.RootPath);

        var runner = new RoutedRunner(new MockLlmProvider([]), def, temp.RootPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }
}
