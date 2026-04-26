using WallyCode.ConsoleApp.Routing;
using WallyCode.ConsoleApp.Sessions;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Routing;

public class SessionTests
{
    [Fact]
    public void Start_initializes_active_step_to_workflow_start()
    {
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "build", "mock-provider", "m", "/src");

        Assert.Equal("requirements", session.WorkflowName);
        Assert.Equal("collect_requirements", session.ActiveStepName);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Equal(0, session.IterationCount);
    }

    [Fact]
    public void Save_then_load_round_trips()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.IterationCount = 3;
        session.LastSelectedKeyword = "[CONTINUE]";
        session.PendingResponses.Add("hi");
        session.Save(temp.RootPath);

        var loaded = Session.Load(temp.RootPath);
        Assert.Equal(3, loaded.IterationCount);
        Assert.Equal("[CONTINUE]", loaded.LastSelectedKeyword);
        Assert.Equal(new[] { "hi" }, loaded.PendingResponses);
    }

    [Fact]
    public void Exists_returns_false_when_no_session_file()
    {
        using var temp = TempWorkspace.Create();
        Assert.False(Session.Exists(temp.RootPath));
    }

    [Fact]
    public void ArchiveCompletedSession_moves_session_contents_into_archive_folder()
    {
        using var temp = TempWorkspace.Create();
        var def = WorkflowDefinition.LoadByName("requirements");
        var session = Session.Start(def, "test goal", "mock-provider", "mock-default-model", temp.RootPath);
        session.Status = SessionStatus.Completed;
        session.Save(temp.RootPath);
        File.WriteAllText(Path.Combine(temp.RootPath, "transcript.log"), "history");

        var archivePath = Session.ArchiveCompletedSession(temp.RootPath);

        Assert.False(Session.Exists(temp.RootPath));
        Assert.True(Directory.Exists(archivePath));
        Assert.StartsWith(Session.ArchiveRoot(temp.RootPath), archivePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(archivePath, "session.json")));
        Assert.True(File.Exists(Path.Combine(archivePath, "transcript.log")));

        var archived = Session.Load(archivePath);
        Assert.Equal(SessionStatus.Completed, archived.Status);
    }

    [Fact]
    public void LoadByName_returns_ask_definition_from_json()
    {
        var definition = WorkflowDefinition.LoadByName("ask");

        Assert.Equal("ask", definition.Name);
        Assert.Equal("prompt", definition.StartStepName);
        Assert.Single(definition.Steps);
        Assert.Equal("prompt", definition.Steps[0].Name);
        Assert.Contains("Do not change files", definition.Steps[0].Instructions);
        Assert.Equal(["[DONE]", "[ERROR]"], definition.Steps[0].AllowedKeywords);
    }

    [Fact]
    public void LoadByName_returns_act_definition_from_json()
    {
        var definition = WorkflowDefinition.LoadByName("act");

        Assert.Equal("act", definition.Name);
        Assert.Equal("prompt", definition.StartStepName);
        Assert.Single(definition.Steps);
        Assert.Equal("prompt", definition.Steps[0].Name);
        Assert.Contains("You may change files", definition.Steps[0].Instructions);
        Assert.Equal(["[DONE]", "[ERROR]"], definition.Steps[0].AllowedKeywords);
    }
}
