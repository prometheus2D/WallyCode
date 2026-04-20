using WallyCode.ConsoleApp;
using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;
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
        var definition = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
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

        Assert.Contains("Active session uses definition 'requirements'", exception.Message);
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
        var definition = RoutingDefinition.LoadByName("requirements");
        var session = RoutedSession.Start(definition, "test goal", "mock-provider", "mock-default-model", workspace.RootPath);
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