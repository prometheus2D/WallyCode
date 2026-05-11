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
    public void Validate_allows_stop_transition_without_target_step()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps = [new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "done", Status = "completed", StopsInvocation = true }] }]
        };

        def.Validate();
    }

    [Fact]
    public void Validate_rejects_terminal_transition_that_does_not_stop()
    {
        var def = new WorkflowDefinition
        {
            Name = "x",
            StartStepName = "u",
            Steps = [new WorkflowStep { Name = "u", Transitions = [new WorkflowTransition { Selection = "done", Status = "completed" }] }]
        };

        Assert.Throws<InvalidOperationException>(() => def.Validate());
    }

    [Fact]
    public void Catalog_resolves_step_transition_ids_from_shared_transitions_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wallycode-workflow-{Guid.NewGuid():N}");
        var steps = Path.Combine(root, "Steps");
        var transitions = Path.Combine(root, "Transitions");
        Directory.CreateDirectory(steps);
        Directory.CreateDirectory(transitions);
        try
        {
            File.WriteAllText(Path.Combine(transitions, "continue.json"),
                """
                {
                  "id": "continue",
                  "selection": "continue"
                }
                """);
            File.WriteAllText(Path.Combine(transitions, "stop.json"),
                """
                {
                  "id": "stop",
                  "selection": "stop",
                  "status": "completed",
                  "stopsInvocation": true
                }
                """);
            File.WriteAllText(Path.Combine(steps, "first.json"),
                """
                {
                  "id": "first",
                  "transitionIds": ["continue", "stop"]
                }
                """);

            var catalog = WorkflowCatalog.LoadFromDirectory(root);
            var definition = catalog.GetDefinition("first");
            var step = definition.GetStep("first");

            Assert.Contains(step.Transitions, transition => transition.Selection == "continue" && transition.TargetStepName is null);
            Assert.Contains(step.Transitions, transition => transition.Selection == "stop" && transition.Status == "completed" && transition.StopsInvocation);
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
    public void Catalog_rejects_unknown_shared_transition_id()
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
                  "transitionIds": ["missing"]
                }
                """);

            var ex = Assert.Throws<InvalidOperationException>(() => WorkflowCatalog.LoadFromDirectory(root));
            Assert.Contains("unknown shared transition 'missing'", ex.Message);
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
        Assert.All(def.Steps, step => Assert.NotEmpty(step.Transitions));
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
