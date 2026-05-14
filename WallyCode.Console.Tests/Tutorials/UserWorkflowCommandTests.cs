using System.Text.Json;
using WallyCode.ConsoleApp;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Tests.Infrastructure;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class UserWorkflowCommandTests
{
    private const string CheapestDefaultModel = "claude-haiku-4.5";

    [Fact]
    public async Task SetupCleanupRecreatesStateAndActivePointerWithoutRemovingProjectFiles()
    {
        using var workspace = CommandTestWorkspace.Create();
        Directory.CreateDirectory(workspace.ProjectRoot);
        var projectFile = Path.Combine(workspace.ProjectRoot, "keep.txt");
        File.WriteAllText(projectFile, "project file");
        Directory.CreateDirectory(workspace.RuntimeRoot);
        File.WriteAllText(ProjectSettings.GetFilePath(workspace.ProjectRoot), "{}");

        var exitCode = await workspace.RunAsync("setup", "--source", workspace.ProjectRoot, "--cleanup");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(projectFile));
        Assert.True(File.Exists(ProjectSettings.GetFilePath(workspace.ProjectRoot)));
        Assert.True(Directory.Exists(workspace.RuntimeRoot));
        Assert.Equal(workspace.ProjectRoot, ProjectSettings.ResolveActiveProjectPath(workspace.AppRoot));
    }

    [Fact]
    public async Task AskAndActAliasesUseMockProviderThroughUserCommandPath()
    {
        using var workspace = CommandTestWorkspace.Create();
        await workspace.RunSetupAsync();

        workspace.Provider.RegisterResponse(Response("stop", "Answered", ("answer", "The project is empty.")));

        var askExitCode = await workspace.RunAsync("ask", "What does this project do?");

        Assert.Equal(0, askExitCode);
        var askSession = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("ask", askSession.WorkflowName);
        Assert.Equal(SessionStatus.Completed, askSession.Status);
        Assert.Equal("The project is empty.", askSession.Memory["answer"]);
        Assert.Single(workspace.Provider.Requests);
        Assert.Equal(workspace.ProjectRoot, workspace.Provider.Requests[0].SourcePath);
        Assert.Contains("Workflow: ask", workspace.Provider.Requests[0].Prompt);

        workspace.Provider.RegisterResponse(Response("stop", "Changed", ("result", "Added README.")));

        var actExitCode = await workspace.RunAsync("act", "Add a README");

        Assert.Equal(0, actExitCode);
        var actSession = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("act", actSession.WorkflowName);
        Assert.Equal(SessionStatus.Completed, actSession.Status);
        Assert.Equal("Added README.", actSession.Memory["result"]);
        Assert.Equal(2, workspace.Provider.Requests.Count);
        Assert.Equal(workspace.ProjectRoot, workspace.Provider.Requests[1].SourcePath);
        Assert.Contains("Workflow: act", workspace.Provider.Requests[1].Prompt);
        Assert.True(Directory.Exists(Session.ArchiveRoot(workspace.RuntimeRoot)));
    }

    [Fact]
    public async Task RequirementsWorkflowLoopsThroughRequirementsTasksAndExecutionWithMockProvider()
    {
        using var workspace = CommandTestWorkspace.Create();
        await workspace.RunSetupAsync();

        workspace.Provider.RegisterResponse(Response("produce_tasks", "Requirements clear", ("requirements", "Build browser Tic Tac Toe.")));

        var runExitCode = await workspace.RunAsync("run", "Build browser Tic Tac Toe", "requirements", "--max-run-iterations", "1");

        Assert.Equal(0, runExitCode);
        var afterRequirements = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("requirements", afterRequirements.WorkflowName);
        Assert.Equal("produce_tasks", afterRequirements.ActiveStepName);
        Assert.Equal(SessionStatus.Active, afterRequirements.Status);
        Assert.Equal(CheapestDefaultModel, afterRequirements.Model);
        Assert.Equal("Build browser Tic Tac Toe.", afterRequirements.Memory["requirements"]);

        workspace.Provider.RegisterResponse(Response("execute_tasks", "Tasks ready", ("tasks", "Create index.html, styles.css, game.js, and README.md.")));

        var firstResumeExitCode = await workspace.RunAsync("resume", "--max-run-iterations", "1");

        Assert.Equal(0, firstResumeExitCode);
        var afterTasks = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("execute_tasks", afterTasks.ActiveStepName);
        Assert.Equal(SessionStatus.Active, afterTasks.Status);
        Assert.Equal("Build browser Tic Tac Toe.", afterTasks.Memory["requirements"]);
        Assert.Equal("Create index.html, styles.css, game.js, and README.md.", afterTasks.Memory["tasks"]);

        workspace.Provider.RegisterResponse(Response("stop", "Execution done", ("execution", "Created the Tic Tac Toe files.")));

        var secondResumeExitCode = await workspace.RunAsync("resume", "--max-run-iterations", "1");

        Assert.Equal(0, secondResumeExitCode);
        var completed = Session.Load(workspace.RuntimeRoot);
        Assert.Equal("execute_tasks", completed.ActiveStepName);
        Assert.Equal(SessionStatus.Completed, completed.Status);
        Assert.Equal("stop", completed.LastSelectedStep);
        Assert.Equal("Created the Tic Tac Toe files.", completed.Memory["execution"]);
        Assert.Equal(3, workspace.Provider.Requests.Count);
        Assert.All(workspace.Provider.Requests, request => Assert.Equal(CheapestDefaultModel, request.Model));
        Assert.Contains("Active step: collect_requirements", workspace.Provider.Requests[0].Prompt);
        Assert.Contains("Active step: produce_tasks", workspace.Provider.Requests[1].Prompt);
        Assert.Contains("requirements: Build browser Tic Tac Toe.", workspace.Provider.Requests[1].Prompt);
        Assert.Contains("Active step: execute_tasks", workspace.Provider.Requests[2].Prompt);
        Assert.Contains("tasks: Create index.html, styles.css, game.js, and README.md.", workspace.Provider.Requests[2].Prompt);
    }

    private static string Response(string selectedStep, string summary, params (string Key, string Value)[] memory)
    {
        return JsonSerializer.Serialize(new
        {
            selectedStep,
            summary,
            memory = memory.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
        });
    }

    private sealed class CommandTestWorkspace : IDisposable
    {
        private CommandTestWorkspace(string rootPath)
        {
            RootPath = rootPath;
            AppRoot = Path.Combine(rootPath, "exe");
            ProjectRoot = Path.Combine(rootPath, "project");
            RuntimeRoot = Path.Combine(ProjectRoot, ".wallycode");
            Directory.CreateDirectory(AppRoot);

            Provider = new TestLlmProvider
            {
                Name = ProviderRegistry.DefaultProviderName,
                DefaultModel = CheapestDefaultModel,
                SupportedModels = [CheapestDefaultModel]
            };
            Registry = new ProviderRegistry([Provider]);
        }

        public string RootPath { get; }

        public string AppRoot { get; }

        public string ProjectRoot { get; }

        public string RuntimeRoot { get; }

        public TestLlmProvider Provider { get; }

        public ProviderRegistry Registry { get; }

        public static CommandTestWorkspace Create()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "WallyCode.Tests", Guid.NewGuid().ToString("N"));
            return new CommandTestWorkspace(rootPath);
        }

        public Task<int> RunSetupAsync()
        {
            return RunAsync("setup", "--source", ProjectRoot);
        }

        public Task<int> RunAsync(params string[] args)
        {
            return Program.RunAsync(args, CancellationToken.None, AppRoot, Registry);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}