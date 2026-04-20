using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Commands;

[Collection("Console")]
public class WorkingDirectoryBehaviorTests
{
    [Fact]
    public async Task Prompt_uses_the_current_working_directory_for_settings_and_runtime_artifacts()
    {
        using var workspace = TempWorkspace.Create();
        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = "prompt result",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);
        var handler = new PromptCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(workspace.RootPath);

        var exitCode = await RunInWorkingDirectoryAsync(
            workspace.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                new PromptCommandOptions { Prompt = "Summarize this repository." },
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        AssertRuntimeFilesExist(Path.Combine(workspace.RootPath, ".wallycode"));
        provider.AssertConsumed();
    }

    [Fact]
    public async Task Source_option_still_overrides_the_current_working_directory()
    {
        using var currentDirectory = TempWorkspace.Create();
        using var sourceWorkspace = TempWorkspace.Create();
        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = "prompt result",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = sourceWorkspace.RootPath
            }
        ]);
        var handler = new PromptCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(sourceWorkspace.RootPath);

        var exitCode = await RunInWorkingDirectoryAsync(
            currentDirectory.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                new PromptCommandOptions
                {
                    Prompt = "Summarize this repository.",
                    SourcePath = sourceWorkspace.RootPath
                },
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        AssertRuntimeFilesExist(Path.Combine(sourceWorkspace.RootPath, ".wallycode"));
        Assert.False(Directory.Exists(Path.Combine(currentDirectory.RootPath, ".wallycode")));
        provider.AssertConsumed();
    }

    [Fact]
    public async Task Prompt_memory_root_override_moves_runtime_artifacts_out_of_the_source_workspace()
    {
        using var workspace = TempWorkspace.Create();
        using var runtime = TempWorkspace.Create();
        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = "prompt result",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);
        var handler = new PromptCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(workspace.RootPath);

        var exitCode = await RunInWorkingDirectoryAsync(
            workspace.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                new PromptCommandOptions
                {
                    Prompt = "Summarize this repository.",
                    MemoryRoot = runtime.RootPath
                },
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        AssertRuntimeFilesExist(runtime.RootPath);
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, ".wallycode")));
        provider.AssertConsumed();
    }

    [Fact]
    public async Task Loop_memory_root_override_writes_session_state_under_the_override_path()
    {
        using var workspace = TempWorkspace.Create();
        using var runtime = TempWorkspace.Create();
        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"done"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspace.RootPath
            }
        ]);
        var handler = new LoopCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(workspace.RootPath);

        var exitCode = await RunInWorkingDirectoryAsync(
            workspace.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                new LoopCommandOptions
                {
                    Goal = "Complete the task.",
                    Definition = "requirements",
                    MemoryRoot = runtime.RootPath,
                    Steps = 1
                },
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        Assert.True(RoutedSession.Exists(runtime.RootPath));
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, ".wallycode")));

        var session = RoutedSession.Load(runtime.RootPath);
        Assert.Equal(workspace.RootPath, session.SourcePath);
        provider.AssertConsumed();
    }

    [Fact]
    public async Task One_process_can_operate_against_two_working_directories_without_touching_the_install_location()
    {
        using var install = TempWorkspace.Create();
        using var workspaceA = TempWorkspace.Create();
        using var workspaceB = TempWorkspace.Create();
        var provider = new MockLlmProvider([
            new MockInvocation
            {
                RawOutput = "first result",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspaceA.RootPath
            },
            new MockInvocation
            {
                RawOutput = "second result",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspaceB.RootPath
            }
        ]);
        var handler = new PromptCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(workspaceA.RootPath);
        WriteMockSettings(workspaceB.RootPath);

        var firstExitCode = await RunInWorkingDirectoryAsync(
            workspaceA.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                new PromptCommandOptions { Prompt = "Summarize workspace A." },
                CancellationToken.None)));

        var secondExitCode = await RunInWorkingDirectoryAsync(
            workspaceB.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                new PromptCommandOptions { Prompt = "Summarize workspace B." },
                CancellationToken.None)));

        Assert.Equal(0, firstExitCode);
        Assert.Equal(0, secondExitCode);
        AssertRuntimeFilesExist(Path.Combine(workspaceA.RootPath, ".wallycode"));
        AssertRuntimeFilesExist(Path.Combine(workspaceB.RootPath, ".wallycode"));
        Assert.False(File.Exists(ProjectSettings.GetFilePath(install.RootPath)));
        Assert.False(Directory.Exists(Path.Combine(install.RootPath, ".wallycode")));
        provider.AssertConsumed();
    }

    private static ProviderRegistry NewRegistry(MockLlmProvider provider)
    {
        return new ProviderRegistry([provider]);
    }

    private static void WriteMockSettings(string workspaceRoot)
    {
        new ProjectSettings
        {
            Provider = "mock-provider",
            Model = "mock-default-model"
        }.Save(workspaceRoot);
    }

    private static void AssertRuntimeFilesExist(string runtimeRoot)
    {
        Assert.Single(Directory.GetFiles(Path.Combine(runtimeRoot, "logs")));
        Assert.Single(Directory.GetFiles(Path.Combine(runtimeRoot, "prompts")));
        Assert.Single(Directory.GetFiles(Path.Combine(runtimeRoot, "raw")));
    }

    private static async Task<T> RunInWorkingDirectoryAsync<T>(string workingDirectory, Func<Task<T>> action)
    {
        var originalDirectory = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = workingDirectory;
            return await action();
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
        }
    }

    private static async Task<int> ExecuteSilentlyAsync(Func<Task<int>> action)
    {
        var writer = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(writer);
            return await action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}