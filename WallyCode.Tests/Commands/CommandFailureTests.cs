using WallyCode.ConsoleApp;
using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Commands;

[Collection("Console")]
public class CommandFailureTests
{
    [Fact]
    public async Task Setup_with_invalid_directory_returns_a_clear_error()
    {
        using var install = TempWorkspace.Create();
        var invalidTarget = Path.Combine(install.RootPath, "bad<path");

        var (exitCode, output) = await ExecuteProgramAsync(
            ["setup", "--directory", invalidTarget],
            install.RootPath,
            install.RootPath);

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid setup target", output);
    }

    [Fact]
    public void Ask_to_loop_options_preserves_logging_flags()
    {
        var options = new AskCommandOptions
        {
            Goal = "hello",
            Log = true,
            Verbose = true
        };

        var loop = options.ToLoopOptions();

        Assert.True(loop.Log);
        Assert.True(loop.Verbose);
    }

    [Fact]
    public void Act_to_loop_options_preserves_logging_flags()
    {
        var options = new ActCommandOptions
        {
            Goal = "hello",
            Log = true,
            Verbose = true
        };

        var loop = options.ToLoopOptions();

        Assert.True(loop.Log);
        Assert.True(loop.Verbose);
    }

    [Fact]
    public async Task Loop_with_conflicting_session_definition_throws_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        var definition = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Save(Path.Combine(workspace.RootPath, ".wallycode"));

        var handler = new LoopCommandHandler(NewRegistry(), new AppLogger());
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            new LoopCommandOptions
            {
                Definition = "ask",
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None));

        Assert.Contains("Active session uses workflow 'requirements'", exception.Message);
    }

    [Fact]
    public async Task Loop_step_flag_forces_a_single_iteration()
    {
        using var workspace = TempWorkspace.Create();
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspace.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"first"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            },
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"second"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        var handler = new LoopCommandHandler(new ProviderRegistry([provider]), new AppLogger());
        var exitCode = await handler.ExecuteAsync(
            new LoopCommandOptions
            {
                Goal = "new goal",
                Definition = "requirements",
                SourcePath = workspace.RootPath,
                Steps = 5,
                Step = true
            },
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        var session = Session.Load(Path.Combine(workspace.RootPath, ".wallycode"));
        Assert.Equal(1, session.IterationCount);
        Assert.Equal("collect_requirements", session.ActiveStepName);
        Assert.Equal(1, provider.ConsumedCount);
    }

    [Fact]
    public async Task Resume_without_an_active_session_throws_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        var handler = new ResumeCommandHandler(new LoopCommandHandler(NewRegistry(), new AppLogger()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            new ResumeCommandOptions
            {
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None));

        Assert.Contains("No resumable session exists", exception.Message);
    }

    [Fact]
    public async Task Resume_with_blocked_session_throws_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        var definition = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Status = SessionStatus.Blocked;
        session.Save(Path.Combine(workspace.RootPath, ".wallycode"));

        var handler = new ResumeCommandHandler(new LoopCommandHandler(NewRegistry(), new AppLogger()));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            new ResumeCommandOptions
            {
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None));

        Assert.Contains("Session is waiting for user input", exception.Message);
    }

    [Fact]
    public async Task Resume_with_terminal_session_throws_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        var definition = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Status = SessionStatus.Completed;
        session.Save(Path.Combine(workspace.RootPath, ".wallycode"));

        var handler = new ResumeCommandHandler(new LoopCommandHandler(NewRegistry(), new AppLogger()));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            new ResumeCommandOptions
            {
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None));

        Assert.Contains("cannot be resumed", exception.Message);
    }

    [Fact]
    public async Task Resume_with_active_session_continues_where_it_left_off()
    {
        using var workspace = TempWorkspace.Create();
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspace.RootPath);

        var definition = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Save(Path.Combine(workspace.RootPath, ".wallycode"));

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[REQUIREMENTS_READY]","summary":"moving forward"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        var handler = new ResumeCommandHandler(new LoopCommandHandler(new ProviderRegistry([provider]), new AppLogger()));
        var exitCode = await handler.ExecuteAsync(
            new ResumeCommandOptions
            {
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        var resumed = Session.Load(Path.Combine(workspace.RootPath, ".wallycode"));
        Assert.Equal("produce_tasks", resumed.ActiveStepName);
        Assert.Equal(1, resumed.IterationCount);
        provider.AssertConsumed();
    }

    [Fact]
    public async Task Resume_step_flag_forces_a_single_iteration()
    {
        using var workspace = TempWorkspace.Create();
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspace.RootPath);

        var definition = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Save(Path.Combine(workspace.RootPath, ".wallycode"));

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"first"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            },
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[CONTINUE]","summary":"second"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        var handler = new ResumeCommandHandler(new LoopCommandHandler(new ProviderRegistry([provider]), new AppLogger()));
        var exitCode = await handler.ExecuteAsync(
            new ResumeCommandOptions
            {
                SourcePath = workspace.RootPath,
                Steps = 5,
                Step = true
            },
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        var resumed = Session.Load(Path.Combine(workspace.RootPath, ".wallycode"));
        Assert.Equal(1, resumed.IterationCount);
        Assert.Equal("collect_requirements", resumed.ActiveStepName);
        Assert.Equal(1, provider.ConsumedCount);
    }

    [Fact]
    public async Task Respond_without_an_active_session_throws_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        var handler = new RespondCommandHandler(new AppLogger());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            new RespondCommandOptions
            {
                Response = "hello",
                SourcePath = workspace.RootPath
            },
            CancellationToken.None));

        Assert.Contains("No active session at", exception.Message);
    }

    [Fact]
    public async Task Provider_set_with_unknown_provider_throws_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        var handler = new ProviderCommandHandler(NewRegistry(), new AppLogger());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            new ProviderCommandOptions
            {
                Name = "missing-provider",
                Set = true,
                SourcePath = workspace.RootPath
            },
            CancellationToken.None));

        Assert.Contains("Unknown provider 'missing-provider'", exception.Message);
    }

    [Fact]
    public async Task Provider_model_with_unsupported_value_returns_a_clear_error()
    {
        using var workspace = TempWorkspace.Create();
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspace.RootPath);

        var handler = new ProviderCommandHandler(NewRegistry(), new AppLogger());

        var (exitCode, output) = await ExecuteSilentlyAsync(() => handler.ExecuteAsync(
            new ProviderCommandOptions
            {
                Name = "mock-provider",
                Model = "unsupported-model",
                SourcePath = workspace.RootPath
            },
            CancellationToken.None));

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown model 'unsupported-model'", output);
    }

    [Fact]
    public async Task Provider_refresh_persists_discovered_models_to_project_settings()
    {
        using var workspace = TempWorkspace.Create();
        var handler = new ProviderCommandHandler(NewRegistry(), new AppLogger());

        var exitCode = await handler.ExecuteAsync(
            new ProviderCommandOptions
            {
                Name = "mock-provider",
                Refresh = true,
                SourcePath = workspace.RootPath
            },
            CancellationToken.None);

        Assert.Equal(0, exitCode);

        var settings = ProjectSettings.Load(workspace.RootPath);
        var catalog = Assert.Single(settings.ProviderCatalog.Providers);
        Assert.Equal("mock-provider", catalog.Name);
        Assert.Contains(catalog.Models, model => model.Name == "mock-default-model");
        Assert.Contains(catalog.Models, model => model.Name == "mock-alt-model");
        Assert.NotNull(catalog.RefreshedAtUtc);
    }

    [Fact]
    public async Task Provider_set_uses_preferred_catalog_model_when_available()
    {
        using var workspace = TempWorkspace.Create();
        var settings = new ProjectSettings();
        settings.ProviderCatalog.Providers.Add(new ProviderCatalogEntry
        {
            Name = "mock-provider",
            PreferredCheapModel = "mock-alt-model"
        });
        settings.Save(workspace.RootPath);

        var handler = new ProviderCommandHandler(NewRegistry(), new AppLogger());
        var exitCode = await handler.ExecuteAsync(
            new ProviderCommandOptions
            {
                Name = "mock-provider",
                Set = true,
                SourcePath = workspace.RootPath
            },
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        var updated = ProjectSettings.Load(workspace.RootPath);
        Assert.Equal("mock-provider", updated.Provider);
        Assert.Equal("mock-alt-model", updated.Model);
    }

    [Fact]
    public async Task Loop_with_blocked_session_and_no_response_warns_and_exits_cleanly()
    {
        using var workspace = TempWorkspace.Create();
        var definition = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Status = SessionStatus.Blocked;
        session.Save(Path.Combine(workspace.RootPath, ".wallycode"));

        var handler = new LoopCommandHandler(NewRegistry(), new AppLogger());
        var (exitCode, output) = await ExecuteSilentlyAsync(() => handler.ExecuteAsync(
            new LoopCommandOptions
            {
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None));

        Assert.Equal(0, exitCode);
        Assert.Contains("Session is blocked. Use 'respond' to provide input.", output);
    }

    [Fact]
    public async Task Loop_with_empty_summary_prints_standard_placeholder()
    {
        using var workspace = TempWorkspace.Create();
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspace.RootPath);

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[DONE]","summary":""}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        var handler = new LoopCommandHandler(new ProviderRegistry([provider]), new AppLogger());
        var (exitCode, output) = await ExecuteSilentlyAsync(() => handler.ExecuteAsync(
            new LoopCommandOptions
            {
                Goal = "new goal",
                Definition = "ask",
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None));

        Assert.Equal(0, exitCode);
        Assert.Contains("Summary: [no summary provided]", output);
    }

    [Fact]
    public async Task Loop_with_completed_session_and_new_goal_archives_old_session_and_starts_a_new_one()
    {
        using var workspace = TempWorkspace.Create();
        var sessionRoot = Path.Combine(workspace.RootPath, ".wallycode");
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspace.RootPath);

        var definition = WorkflowDefinition.LoadByName("ask");
        var session = Session.Start(definition, "old goal", "mock-provider", "mock-default-model", workspace.RootPath);
        session.Status = SessionStatus.Completed;
        session.Save(sessionRoot);
        File.WriteAllText(Path.Combine(sessionRoot, "transcript.log"), "old transcript");

        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"done again"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);

        var handler = new LoopCommandHandler(new ProviderRegistry([provider]), new AppLogger());
        var exitCode = await handler.ExecuteAsync(
            new LoopCommandOptions
            {
                Goal = "new goal",
                Definition = "ask",
                SourcePath = workspace.RootPath,
                Steps = 1
            },
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.True(Session.Exists(sessionRoot));

        var active = Session.Load(sessionRoot);
        Assert.Equal("new goal", active.Goal);
        Assert.Equal(SessionStatus.Completed, active.Status);

        var archiveRoot = Session.ArchiveRoot(sessionRoot);
        var archivedSessionFolder = Assert.Single(Directory.GetDirectories(archiveRoot));
        var archived = Session.Load(archivedSessionFolder);
        Assert.Equal("old goal", archived.Goal);
        Assert.True(File.Exists(Path.Combine(archivedSessionFolder, "transcript.log")));
        provider.AssertConsumed();
    }

    private static ProviderRegistry NewRegistry()
    {
        return new ProviderRegistry([new MockLlmProvider([])]);
    }

    private static async Task<(int ExitCode, string Output)> ExecuteProgramAsync(string[] args, string appDirectoryPath, string workingDirectory)
    {
        var originalDirectory = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = workingDirectory;
            return await ExecuteSilentlyAsync(() => Program.RunAsync(args, CancellationToken.None, appDirectoryPath));
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
        }
    }

    private static async Task<(int ExitCode, string Output)> ExecuteSilentlyAsync(Func<Task<int>> action)
    {
        var writer = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(writer);
            var exitCode = await action();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}