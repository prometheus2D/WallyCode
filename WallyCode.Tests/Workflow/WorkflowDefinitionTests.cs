using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.Tests.Workflow;

public class WorkflowDefinitionTests
{
    [Fact]
    public void Validate_passes_for_minimal_definition()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "u",
                    AllowedKeywords = ["[DONE]"],
                    KeywordOptions = [new() { Keyword = "[DONE]", Description = "Complete the flow." }]
                }
            ]
        };
        def.Validate();
    }

    [Fact]
    public void Validate_rejects_missing_start_step()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "missing",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "u",
                    AllowedKeywords = ["[DONE]"],
                    KeywordOptions = [new() { Keyword = "[DONE]", Description = "Complete the flow." }]
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_transition_to_unknown_step()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "u",
                    AllowedKeywords = ["[NEXT]"],
                    KeywordOptions = [new() { Keyword = "[NEXT]", Description = "Move to the next step." }],
                    Transitions = new() { ["[NEXT]"] = "ghost" }
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_transition_key_not_in_allowed_keywords()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "u",
                    AllowedKeywords = ["[DONE]"],
                    KeywordOptions = [new() { Keyword = "[DONE]", Description = "Complete the flow." }],
                    Transitions = new() { ["[NEXT]"] = "u" }
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_allowed_keyword_without_description()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps = [new WorkflowStep { Name = "u", AllowedKeywords = ["[DONE]"] }]
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
        var def = WorkflowDefinition.LoadByName(name);
        Assert.Equal(name, def.Name);
        Assert.Contains(def.Steps, step => step.Name == def.StartStepName);
    }
}
