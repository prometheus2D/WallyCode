using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Routing;

public class RoutedRunnerTests
{
    private static RoutedRunner NewRunner(string root, RoutingDefinition def, params MockInvocation[] script)
    {
        var session = RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", root);
        session.Save(root);
        return new RoutedRunner(new MockLlmProvider(script), def, root);
    }

    [Fact]
    public async Task Continue_keeps_active_unit_and_increments_iteration()
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"working"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("[CONTINUE]", result.SelectedKeyword);
        Assert.Equal("collect_requirements", result.ActiveUnitName);
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
        var def = RoutingDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = """{"selectedKeyword":"[REQUIREMENTS_READY]"}""" });

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal("produce_tasks", result.ActiveUnitName);
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
        var def = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        if (keyword is "[DONE]" or "[ERROR]")
        {
            session.ActiveUnitName = "produce_tasks";
        }
        session.Save(temp.RootPath);

        var runner = new RoutedRunner(
            new MockLlmProvider([new MockInvocation { RawOutput = $$"""{"selectedKeyword":"{{keyword}}","summary":"problem details"}""" }]),
            def, temp.RootPath);

        var result = await runner.RunOnceAsync(CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.StopsInvocation);
        Assert.Equal("problem details", RoutedSession.Load(temp.RootPath).LastSummary);
    }

    [Theory]
    [InlineData("DONE")]
    [InlineData("[BOGUS]")]
    [InlineData("[FAIL]")]
    public async Task Invalid_keyword_throws(string keyword)
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.ActiveUnitName = "produce_tasks";
        session.Save(temp.RootPath);

        var runner = new RoutedRunner(
            new MockLlmProvider([new MockInvocation { RawOutput = $$"""{"selectedKeyword":"{{keyword}}"}""" }]),
            def, temp.RootPath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task Malformed_or_missing_keyword_json_throws(string rawOutput)
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = rawOutput });

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Code_fenced_json_is_parsed()
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var runner = NewRunner(temp.RootPath, def,
            new MockInvocation { RawOutput = "```json\n{\"selectedKeyword\":\"[CONTINUE]\"}\n```" });

        var result = await runner.RunOnceAsync(CancellationToken.None);
        Assert.Equal("[CONTINUE]", result.SelectedKeyword);
    }

    [Fact]
    public async Task Run_steps_stops_on_blocking_keyword()
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
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
        var def = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
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
        var def = RoutingDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new RoutedRunner(provider, def, temp.RootPath);
        RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Keyword options:", provider.Requests[0].Prompt);
        Assert.Contains("[REQUIREMENTS_READY]:", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_mentions_error_keyword_guidance()
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new RoutedRunner(provider, def, temp.RootPath);
        RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("select [ERROR]", provider.Requests[0].Prompt);
        Assert.DoesNotContain("[FAIL]", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_does_not_include_global_prompt_when_not_configured()
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new RoutedRunner(provider, def, temp.RootPath);
        RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

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

        var def = RoutingDefinition.LoadByName("requirements");
        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[CONTINUE]"}""" }
        ]);
        var runner = new RoutedRunner(provider, def, temp.RootPath);
        RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath).Save(temp.RootPath);

        await runner.RunOnceAsync(CancellationToken.None);

        Assert.Contains("Global prompt:", provider.Requests[0].Prompt);
        Assert.Contains("Always preserve brackets in keywords.", provider.Requests[0].Prompt);
    }

    [Fact]
    public async Task Run_throws_when_session_definition_does_not_match()
    {
        using var temp = TempWorkspace.Create();
        var def = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.DefinitionName = "other";
        session.Save(temp.RootPath);

        var runner = new RoutedRunner(new MockLlmProvider([]), def, temp.RootPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunOnceAsync(CancellationToken.None));
    }
}
