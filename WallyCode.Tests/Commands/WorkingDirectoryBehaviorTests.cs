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
    public async Task Ask_uses_the_current_working_directory_for_settings_and_runtime_artifacts()
    {
        using var workspace = TempWorkspace.Create();
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
                NewAskOptions("Summarize this repository."),
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        AssertSessionExists(Path.Combine(workspace.RootPath, ".wallycode"), workspace.RootPath, "ask");
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
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"done"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = sourceWorkspace.RootPath
            }
        ]);
        var handler = new LoopCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(sourceWorkspace.RootPath);

        var exitCode = await RunInWorkingDirectoryAsync(
            currentDirectory.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                NewAskOptions("Summarize this repository.", sourceWorkspace.RootPath),
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        AssertSessionExists(Path.Combine(sourceWorkspace.RootPath, ".wallycode"), sourceWorkspace.RootPath, "ask");
        Assert.False(Directory.Exists(Path.Combine(currentDirectory.RootPath, ".wallycode")));
        provider.AssertConsumed();
    }

    [Fact]
    public async Task Ask_memory_root_override_moves_runtime_artifacts_out_of_the_source_workspace()
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
                NewAskOptions("Summarize this repository.", memoryRoot: runtime.RootPath),
                CancellationToken.None)));

        Assert.Equal(0, exitCode);
        AssertSessionExists(runtime.RootPath, workspace.RootPath, "ask");
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
        AssertSessionExists(runtime.RootPath, workspace.RootPath, "requirements");
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, ".wallycode")));
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
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"first result"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspaceA.RootPath
            },
            new MockInvocation
            {
                RawOutput = """{"selectedKeyword":"[DONE]","summary":"second result"}""",
                ExpectedModel = "mock-default-model",
                ExpectedSourcePath = workspaceB.RootPath
            }
        ]);
        var handler = new LoopCommandHandler(NewRegistry(provider), new AppLogger());
        WriteMockSettings(workspaceA.RootPath);
        WriteMockSettings(workspaceB.RootPath);

        var firstExitCode = await RunInWorkingDirectoryAsync(
            workspaceA.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                NewAskOptions("Summarize workspace A."),
                CancellationToken.None)));

        var secondExitCode = await RunInWorkingDirectoryAsync(
            workspaceB.RootPath,
            () => ExecuteSilentlyAsync(() => handler.ExecuteAsync(
                NewAskOptions("Summarize workspace B."),
                CancellationToken.None)));

        Assert.Equal(0, firstExitCode);
        Assert.Equal(0, secondExitCode);
        AssertSessionExists(Path.Combine(workspaceA.RootPath, ".wallycode"), workspaceA.RootPath, "ask");
        AssertSessionExists(Path.Combine(workspaceB.RootPath, ".wallycode"), workspaceB.RootPath, "ask");
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

    private static LoopCommandOptions NewAskOptions(string goal, string? sourcePath = null, string? memoryRoot = null)
    {
        return new AskCommandOptions
        {
            Goal = goal,
            SourcePath = sourcePath,
            MemoryRoot = memoryRoot,
            Steps = 1
        }.ToLoopOptions();
    }

    private static void AssertSessionExists(string runtimeRoot, string expectedSourcePath, string expectedDefinitionName)
    {
        Assert.True(RoutedSession.Exists(runtimeRoot));
        var session = RoutedSession.Load(runtimeRoot);
        Assert.Equal(expectedSourcePath, session.SourcePath);
        Assert.Equal(expectedDefinitionName, session.DefinitionName);
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