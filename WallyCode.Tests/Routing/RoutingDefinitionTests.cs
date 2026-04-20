using WallyCode.ConsoleApp.Routing;

namespace WallyCode.Tests.Routing;

public class RoutingDefinitionTests
{
    [Fact]
    public void Validate_passes_for_minimal_definition()
    {
        var def = new RoutingDefinition
        {
            Name = "x",
            StartUnitName = "u",
            Units = [new LogicalUnit { Name = "u", AllowedKeywords = ["[DONE]"] }]
        };
        def.Validate();
    }

    [Fact]
    public void Validate_rejects_missing_start_unit()
    {
        var def = new RoutingDefinition
        {
            Name = "x",
            StartUnitName = "missing",
            Units = [new LogicalUnit { Name = "u", AllowedKeywords = ["[DONE]"] }]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_transition_to_unknown_unit()
    {
        var def = new RoutingDefinition
        {
            Name = "x",
            StartUnitName = "u",
            Units =
            [
                new LogicalUnit
                {
                    Name = "u",
                    AllowedKeywords = ["[NEXT]"],
                    Transitions = new() { ["[NEXT]"] = "ghost" }
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_transition_key_not_in_allowed_keywords()
    {
        var def = new RoutingDefinition
        {
            Name = "x",
            StartUnitName = "u",
            Units =
            [
                new LogicalUnit
                {
                    Name = "u",
                    AllowedKeywords = ["[DONE]"],
                    Transitions = new() { ["[NEXT]"] = "u" }
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Theory]
    [InlineData("ask")]
    [InlineData("act")]
    [InlineData("requirements")]
    [InlineData("tasks")]
    [InlineData("full-pipeline")]
    public void Shipped_definitions_load_and_validate(string name)
    {
        var def = RoutingDefinition.LoadByName(name);
        Assert.Equal(name, def.Name);
        Assert.Contains(def.Units, u => u.Name == def.StartUnitName);
    }
}
