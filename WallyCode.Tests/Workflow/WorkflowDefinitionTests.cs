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
                    Transitions = [new() { Keyword = "[DONE]", Description = "Complete the flow." }]
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
                    Transitions = [new() { Keyword = "[DONE]", Description = "Complete the flow." }]
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
                    Transitions = [new() { Keyword = "[NEXT]", Description = "Move to the next step.", NextStep = "ghost" }]
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_duplicate_transition_keywords()
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
                    Transitions =
                    [
                        new() { Keyword = "[DONE]", Description = "Complete the flow." },
                        new() { Keyword = "[DONE]", Description = "Complete the flow again." }
                    ]
                }
            ]
        };
        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_transition_without_description()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps = [new WorkflowStep { Name = "u", Transitions = [new() { Keyword = "[DONE]" }] }]
        };

        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Theory]
    [InlineData("ask")]
    [InlineData("act")]
    [InlineData("collect_requirements")]
    [InlineData("produce_tasks")]
    [InlineData("execute_tasks")]
    public void Shipped_steps_load_and_validate(string name)
    {
        var def = WorkflowDefinition.LoadByName(name);
        Assert.Equal(name, def.Name);
        Assert.Contains(def.Steps, step => step.Name == def.StartStepName);
    }

    [Theory]
    [InlineData("requirements", "collect_requirements")]
    [InlineData("tasks", "produce_tasks")]
    [InlineData("full-pipeline", "collect_requirements")]
    public void Legacy_definition_names_resolve_to_start_steps(string legacyName, string startStep)
    {
        var def = WorkflowDefinition.LoadByName(legacyName);

        Assert.Equal(startStep, def.Name);
        Assert.Equal(startStep, def.StartStepName);
    }
}
