using WallyCode.ConsoleApp.Routing;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Routing;

public class RoutedSessionTests
{
    [Fact]
    public void Start_initializes_active_unit_to_definition_start()
    {
        var def = TestDefinitions.TwoUnit();
        var session = RoutedSession.Start(def, "build", "mock-provider", "m", "/src");

        Assert.Equal("test-def", session.DefinitionName);
        Assert.Equal("start", session.ActiveUnitName);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Equal(0, session.IterationCount);
    }

    [Fact]
    public void Save_then_load_round_trips()
    {
        using var temp = TempWorkspace.Create();
        var def = TestDefinitions.TwoUnit();
        var session = TestDefinitions.NewSession(def, temp.RootPath);
        session.IterationCount = 3;
        session.LastSelectedKeyword = "[CONTINUE]";
        session.PendingResponses.Add("hi");
        session.Save(temp.RootPath);

        var loaded = RoutedSession.Load(temp.RootPath);
        Assert.Equal(3, loaded.IterationCount);
        Assert.Equal("[CONTINUE]", loaded.LastSelectedKeyword);
        Assert.Equal(new[] { "hi" }, loaded.PendingResponses);
    }

    [Fact]
    public void Exists_returns_false_when_no_session_file()
    {
        using var temp = TempWorkspace.Create();
        Assert.False(RoutedSession.Exists(temp.RootPath));
    }

    [Fact]
    public void LoadByName_returns_ask_definition_from_json()
    {
        var definition = RoutingDefinition.LoadByName("ask");

        Assert.Equal("ask", definition.Name);
        Assert.Equal("prompt", definition.StartUnitName);
        Assert.Single(definition.Units);
        Assert.Equal("prompt", definition.Units[0].Name);
        Assert.Contains("Do not change files", definition.Units[0].Instructions);
        Assert.Equal(["[DONE]", "[ASK_USER]", "[FAIL]"], definition.Units[0].AllowedKeywords);
    }

    [Fact]
    public void LoadByName_returns_act_definition_from_json()
    {
        var definition = RoutingDefinition.LoadByName("act");

        Assert.Equal("act", definition.Name);
        Assert.Equal("prompt", definition.StartUnitName);
        Assert.Single(definition.Units);
        Assert.Equal("prompt", definition.Units[0].Name);
        Assert.Contains("You may change files", definition.Units[0].Instructions);
        Assert.Equal(["[DONE]", "[ASK_USER]", "[FAIL]"], definition.Units[0].AllowedKeywords);
    }
}
