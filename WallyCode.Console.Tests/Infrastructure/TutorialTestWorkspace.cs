using System.Text.Json;
using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Tests.Infrastructure;

internal sealed class TutorialTestWorkspace : IDisposable
{
    private readonly string _rootPath;

    private TutorialTestWorkspace(string rootPath)
    {
        _rootPath = rootPath;
    }

    public string RootPath => _rootPath;

    public string ProjectRoot => _rootPath;

    public string RuntimeRoot => Path.Combine(_rootPath, ".wallycode");

    public string LoadablesRoot => Path.Combine(_rootPath, "Loadables");

    public static TutorialTestWorkspace Create(bool runSetup = false, bool cleanupBeforeSetup = false)
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "WallyCode.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        var workspace = new TutorialTestWorkspace(rootPath);
        
        if (runSetup)
        {
            workspace.RunSetup(cleanup: cleanupBeforeSetup);
        }
        
        return workspace;
    }

    public void RunSetup(string providerName = "test-provider", string defaultModel = "test-model", bool cleanup = false)
    {
        var testProvider = new TestLlmProvider { Name = providerName, DefaultModel = defaultModel };
        // Include both test provider and the default provider to avoid setup failures
        var defaultProvider = new TestLlmProvider { Name = "gh-copilot-claude", DefaultModel = "claude-3-sonnet" };
        var registry = new ProviderRegistry([testProvider, defaultProvider]);
        var handler = new SetupCommandHandler(registry, new AppLogger(), ProjectRoot);
        handler.ExecuteAsync(new SetupCommandOptions { SourcePath = ProjectRoot, Cleanup = cleanup }, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    public ProjectSettings CreateProjectSettings(string? globalPrompt = null)
    {
        var settings = new ProjectSettings();
        settings.GlobalPrompt = globalPrompt;
        settings.Save(ProjectRoot);
        return settings;
    }

    public void WriteWorkflowCatalogFromSource()
    {
        var sourceRoot = GetSourceLoadablesRoot();
        CopyDirectory(sourceRoot, LoadablesRoot);
    }

    public WorkflowDefinition CreateDefinition(string name, string startStepName, params WorkflowStep[] steps)
    {
        var definition = new WorkflowDefinition
        {
            Name = name,
            StartStepName = startStepName,
            Steps = steps.ToList()
        };

        definition.Validate();
        return definition;
    }

    public Session CreateSession(WorkflowDefinition definition, string goal, string providerName = "test-provider", string? model = "test-model")
    {
        Directory.CreateDirectory(RuntimeRoot);
        var session = Session.Start(definition, goal, providerName, model, ProjectRoot);
        session.Save(RuntimeRoot);
        return session;
    }

    public void SaveJson(string relativePath, object value)
    {
        var path = Path.Combine(_rootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _rootPath);
        File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }

    public string ReadFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(_rootPath, relativePath));
    }

    private static string GetSourceLoadablesRoot()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repositoryRoot, "WallyCode.Console", "Loadables");
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
        foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));
        }

        foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var destinationFile = file.Replace(sourcePath, destinationPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationPath);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }
}
