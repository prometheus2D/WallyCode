using WallyCode.ConsoleApp.Tests.Infrastructure;
using WallyCode.ConsoleApp.Workflow;
using Xunit;

namespace WallyCode.ConsoleApp.Tests.Tutorials;

public sealed class DefinitionsTutorialTests
{
    [Fact]
    public void Workflow_definition_json_validates_and_catalog_compiles_from_workspace_files()
    {
        using var workspace = TutorialTestWorkspace.Create();
        workspace.WriteWorkflowCatalogFromSource();

        var catalog = WorkflowCatalog.LoadFromDirectory(workspace.WorkflowRoot);
        var ask = catalog.GetDefinition("ask");
        var act = catalog.GetDefinition("act");
        var requirements = catalog.GetDefinition("requirements");
        var tasks = catalog.GetDefinition("tasks");

        Assert.Equal("ask", ask.Name);
        Assert.Equal("act", act.Name);
        Assert.Equal("requirements", requirements.Name);
        Assert.Equal("tasks", tasks.Name);
        Assert.Equal("ask", ask.StartStepName);
        Assert.Equal("act", act.StartStepName);
        Assert.Equal("collect_requirements", requirements.StartStepName);
        Assert.Equal("produce_tasks", tasks.StartStepName);
    }

    [Fact]
    public void Invalid_transition_target_is_rejected()
    {
        var definition = new WorkflowDefinition
        {
            Name = "sample",
            StartStepName = "one",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "one",
                    Transitions = [new WorkflowTransition { Selection = "go", TargetStepName = "missing", Status = "active" }]
                }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(definition.Validate);
        Assert.Contains("unknown transition step", exception.Message, StringComparison.Ordinal);
    }
}
