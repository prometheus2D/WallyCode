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
            Steps = [new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "u", TargetStepName = "u" }] }]
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
            Steps = [new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "u", TargetStepName = "u" }] }]
        };

        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Catalog_rejects_transition_to_step_without_loadable_json_definition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wallycode-workflow-{Guid.NewGuid():N}");
        var steps = Path.Combine(root, "Steps");
        Directory.CreateDirectory(steps);
        try
        {
            File.WriteAllText(Path.Combine(steps, "first.json"),
                """
                {
                  "id": "first",
                  "transitions": [
                    { "selection": "missing", "targetStepName": "missing" }
                  ]
                }
                """);

            var ex = Assert.Throws<InvalidOperationException>(() => WorkflowCatalog.LoadFromDirectory(root));
            Assert.Contains("loadable shared step id", ex.Message);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Catalog_transition_targets_must_reference_shared_step_ids_not_custom_names()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wallycode-workflow-{Guid.NewGuid():N}");
        var steps = Path.Combine(root, "Steps");
        Directory.CreateDirectory(steps);
        try
        {
            File.WriteAllText(Path.Combine(steps, "first.json"),
                """
                {
                  "id": "first",
                  "transitions": [
                    { "selection": "custom_second", "targetStepName": "custom_second" }
                  ]
                }
                """);
            File.WriteAllText(Path.Combine(steps, "second.json"),
                """
                {
                  "id": "second",
                  "name": "custom_second",
                  "transitions": [
                    { "selection": "second", "targetStepName": "second" }
                  ]
                }
                """);

            var ex = Assert.Throws<InvalidOperationException>(() => WorkflowCatalog.LoadFromDirectory(root));
            Assert.Contains("loadable shared step id", ex.Message);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Validate_rejects_transition_to_unknown_step()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps = [new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "ghost", TargetStepName = "ghost" }] }]
        };

        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_transition_without_target_step()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps = [new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "done", Status = "completed", StopsInvocation = true }] }]
        };

        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Validate_rejects_duplicate_transition_selections()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps =
            [
                new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "v", TargetStepName = "v" }, new WorkflowTransition { Selection = "v", TargetStepName = "v" }] },
                new WorkflowStep { Name = "v", Transitions = [new WorkflowTransition { Selection = "v", TargetStepName = "v" }] }
            ]
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
