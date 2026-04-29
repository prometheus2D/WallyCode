using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.EndToEnd;

/// <summary>
/// End-to-end tests that drive the real CLI argv pipeline (Program.RunAsync) through
/// multi-step workflows against a temp workspace and a scripted MockLlmProvider.
///
/// These tests prove a user can: setup a workspace, start a session, step through a
/// workflow with --step, respond to ASK_USER prompts, resume, complete, and archive.
/// </summary>
[Collection("Console")]
public class StepwiseWorkflowEndToEndTests
{
    [Fact]
    public async Task Ask_workflow_completes_in_a_single_step_invocation()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"answered"}""",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);

        var run = await harness.InvokeAsync("ask", "Summarize the repo.");

        run.AssertSucceeded();
        Assert.Contains("Selected keyword: [DONE]", run.Output);
        Assert.Contains("Summary: answered", run.Output);

        // session was archived because [DONE] is terminal and the loop archives terminals on next start;
        // for this single invocation the active session.json should still be present and Completed.
        var sessionRoot = Path.Combine(workspace.RootPath, ".wallycode");
        Assert.True(Session.Exists(sessionRoot));
        var session = Session.Load(sessionRoot);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.Equal(1, session.IterationCount);

        provider.AssertConsumed();
    }

    [Fact]
    public async Task Requirements_workflow_steps_through_continue_ask_respond_resume_done()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            // step 1: loop --step -> CONTINUE on collect_requirements
            new MockInvocation
            {
                Label = "step-1-continue",
                RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"gathering requirements"}""",
                ExpectedSourcePath = workspace.RootPath
            },
            // step 2: loop --step -> ASK_USER (blocks the session)
            new MockInvocation
            {
                Label = "step-2-ask",
                RawOutput = """{"selectedKeyword":"[ASK_USER]","summary":"need clarification"}""",
                ExpectedSourcePath = workspace.RootPath
            },
            // step 3: resume --step (after respond) -> REQUIREMENTS_READY transitions to produce_tasks
            new MockInvocation
            {
                Label = "step-3-ready",
                RawOutput = """{"selectedKeyword":"[REQUIREMENTS_READY]","summary":"requirements ready"}""",
                ExpectedSourcePath = workspace.RootPath
            },
            // step 4: resume --step on produce_tasks -> DONE (terminal)
            new MockInvocation
            {
                Label = "step-4-done",
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"all tasks produced"}""",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);
        var sessionRoot = Path.Combine(workspace.RootPath, ".wallycode");

        // 1. start the session with --step
        var step1 = await harness.InvokeAsync("loop", "Build a CSV importer.", "--definition", "requirements", "--step");
        step1.AssertSucceeded();
        Assert.Contains("Selected keyword: [CONTINUE]", step1.Output);
        var afterStep1 = Session.Load(sessionRoot);
        Assert.Equal("collect_requirements", afterStep1.ActiveStepName);
        Assert.Equal(SessionStatus.Active, afterStep1.Status);
        Assert.Equal(1, afterStep1.IterationCount);

        // 2. continue with --step; mock returns ASK_USER which should block the session
        var step2 = await harness.InvokeAsync("loop", "--step");
        step2.AssertSucceeded();
        Assert.Contains("Selected keyword: [ASK_USER]", step2.Output);
        var afterStep2 = Session.Load(sessionRoot);
        Assert.Equal(SessionStatus.Blocked, afterStep2.Status);

        // 2b. running loop again on a blocked session should warn and exit cleanly without consuming a script entry
        var blockedRun = await harness.InvokeAsync("loop", "--step");
        blockedRun.AssertSucceeded();
        Assert.Contains("Session is blocked", blockedRun.Output);

        // 3. respond unblocks the session and queues the response for the next prompt
        var respond = await harness.InvokeAsync("respond", "use comma-separated values");
        respond.AssertSucceeded();
        var afterRespond = Session.Load(sessionRoot);
        Assert.Equal(SessionStatus.Active, afterRespond.Status);
        Assert.Contains("use comma-separated values", afterRespond.PendingResponses);

        // 4. resume --step -> REQUIREMENTS_READY transitions to produce_tasks
        var step3 = await harness.InvokeAsync("resume", "--step");
        step3.AssertSucceeded();
        Assert.Contains("Selected keyword: [REQUIREMENTS_READY]", step3.Output);
        var afterStep3 = Session.Load(sessionRoot);
        Assert.Equal("produce_tasks", afterStep3.ActiveStepName);
        Assert.Empty(afterStep3.PendingResponses);

        // and the prompt for step 3 should have included the user's pending response
        Assert.Contains(provider.Requests, r => r.Prompt.Contains("use comma-separated values"));

        // 5. resume --step -> DONE (terminal)
        var step4 = await harness.InvokeAsync("resume", "--step");
        step4.AssertSucceeded();
        Assert.Contains("Selected keyword: [DONE]", step4.Output);
        var afterStep4 = Session.Load(sessionRoot);
        Assert.Equal(SessionStatus.Completed, afterStep4.Status);

        provider.AssertConsumed();
    }

    [Fact]
    public async Task Starting_a_new_loop_after_completion_archives_the_previous_session()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation { RawOutput = """{"selectedKeyword":"[DONE]","summary":"first done"}""" },
            new MockInvocation { RawOutput = """{"selectedKeyword":"[DONE]","summary":"second done"}""" }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);
        var sessionRoot = Path.Combine(workspace.RootPath, ".wallycode");

        var first = await harness.InvokeAsync("ask", "First question.");
        first.AssertSucceeded();
        Assert.Equal(SessionStatus.Completed, Session.Load(sessionRoot).Status);

        var second = await harness.InvokeAsync("ask", "Second question.");
        second.AssertSucceeded();

        // active session is the new one
        var active = Session.Load(sessionRoot);
        Assert.Equal("Second question.", active.Goal);
        Assert.Equal(SessionStatus.Completed, active.Status);

        // previous session was archived under .wallycode/archive/
        var archiveRoot = Session.ArchiveRoot(sessionRoot);
        Assert.True(Directory.Exists(archiveRoot));
        var archivedFolder = Assert.Single(Directory.GetDirectories(archiveRoot));
        var archived = Session.Load(archivedFolder);
        Assert.Equal("First question.", archived.Goal);

        provider.AssertConsumed();
    }

    [Fact]
    public async Task Provider_failure_during_loop_marks_session_in_error_and_exits_nonzero()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation { Exception = new InvalidOperationException("provider unreachable") }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);

        var run = await harness.InvokeAsync("loop", "Do something.", "--step");

        Assert.Equal(1, run.ExitCode);
        var session = Session.Load(Path.Combine(workspace.RootPath, ".wallycode"));
        Assert.Equal(SessionStatus.Error, session.Status);
        Assert.Equal("[ERROR]", session.LastSelectedKeyword);
        Assert.Equal("provider unreachable", session.LastSummary);
    }

    [Fact]
    public async Task Act_workflow_completes_in_a_single_step_invocation()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"changes applied"}""",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);

        var run = await harness.InvokeAsync("act", "Apply the change.");

        run.AssertSucceeded();
        Assert.Contains("Selected keyword: [DONE]", run.Output);

        var session = Session.Load(Path.Combine(workspace.RootPath, ".wallycode"));
        Assert.Equal("act", session.WorkflowName);
        Assert.Equal(SessionStatus.Completed, session.Status);
        provider.AssertConsumed();
    }

    [Fact]
    public async Task Tasks_workflow_steps_through_produce_tasks_then_execute_tasks_to_done()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            // step 1: produce_tasks -> TASKS_READY (transitions to execute_tasks)
            new MockInvocation
            {
                Label = "produce-tasks-ready",
                RawOutput = """{"selectedKeyword":"[TASKS_READY]","summary":"task list ready"}""",
                ExpectedSourcePath = workspace.RootPath
            },
            // step 2: execute_tasks -> DONE
            new MockInvocation
            {
                Label = "execute-tasks-done",
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"all tasks executed"}""",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);
        var sessionRoot = Path.Combine(workspace.RootPath, ".wallycode");

        var step1 = await harness.InvokeAsync("loop", "Implement feature X.", "--definition", "tasks", "--step");
        step1.AssertSucceeded();
        Assert.Contains("Selected keyword: [TASKS_READY]", step1.Output);
        var afterStep1 = Session.Load(sessionRoot);
        Assert.Equal("tasks", afterStep1.WorkflowName);
        Assert.Equal("execute_tasks", afterStep1.ActiveStepName);
        Assert.Equal(SessionStatus.Active, afterStep1.Status);

        var step2 = await harness.InvokeAsync("resume", "--step");
        step2.AssertSucceeded();
        Assert.Contains("Selected keyword: [DONE]", step2.Output);
        var afterStep2 = Session.Load(sessionRoot);
        Assert.Equal(SessionStatus.Completed, afterStep2.Status);

        provider.AssertConsumed();
    }

    [Fact]
    public async Task FullPipeline_workflow_runs_all_three_steps_to_done()
    {
        using var workspace = TempWorkspace.Create();
        WriteMockProviderSettings(workspace.RootPath);

        var provider = new MockLlmProvider([
            // collect_requirements -> REQUIREMENTS_READY (transitions to produce_tasks)
            new MockInvocation
            {
                Label = "requirements-ready",
                RawOutput = """{"selectedKeyword":"[REQUIREMENTS_READY]","summary":"requirements ready"}""",
                ExpectedSourcePath = workspace.RootPath
            },
            // produce_tasks -> TASKS_READY (transitions to execute_tasks)
            new MockInvocation
            {
                Label = "tasks-ready",
                RawOutput = """{"selectedKeyword":"[TASKS_READY]","summary":"task list ready"}""",
                ExpectedSourcePath = workspace.RootPath
            },
            // execute_tasks -> DONE
            new MockInvocation
            {
                Label = "execute-done",
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"pipeline complete"}""",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        using var harness = CliHarness.Create(workspace.RootPath, provider);
        var sessionRoot = Path.Combine(workspace.RootPath, ".wallycode");

        var step1 = await harness.InvokeAsync("loop", "End-to-end pipeline goal.", "--definition", "full-pipeline", "--step");
        step1.AssertSucceeded();
        var afterStep1 = Session.Load(sessionRoot);
        Assert.Equal("full-pipeline", afterStep1.WorkflowName);
        Assert.Equal("produce_tasks", afterStep1.ActiveStepName);

        var step2 = await harness.InvokeAsync("resume", "--step");
        step2.AssertSucceeded();
        var afterStep2 = Session.Load(sessionRoot);
        Assert.Equal("execute_tasks", afterStep2.ActiveStepName);

        var step3 = await harness.InvokeAsync("resume", "--step");
        step3.AssertSucceeded();
        Assert.Contains("Selected keyword: [DONE]", step3.Output);
        var afterStep3 = Session.Load(sessionRoot);
        Assert.Equal(SessionStatus.Completed, afterStep3.Status);

        provider.AssertConsumed();
    }

    private static void WriteMockProviderSettings(string workspaceRoot)
    {
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspaceRoot);
    }
}
