using WallyCode.ConsoleApp.Workflow;

namespace WallyCode.ConsoleApp.Tests.Infrastructure;

internal static class WorkflowFixtures
{
    public static WorkflowDefinition AskDefinition()
    {
        return new WorkflowDefinition
        {
            Name = "ask",
            Instructions = "Answer directly in a single response without intending to modify files.",
            StartStepName = "ask",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "ask",
                    Instructions = "Answer the user's request directly, then stop.",
                    WritesMemory = ["answer"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "stop", Status = "completed", StopsInvocation = true },
                        new WorkflowTransition { Selection = "ask_user", Status = "blocked", StopsInvocation = true },
                        new WorkflowTransition { Selection = "error", Status = "error", StopsInvocation = true }
                    ]
                }
            ]
        };
    }

    public static WorkflowDefinition ActDefinition()
    {
        return new WorkflowDefinition
        {
            Name = "act",
            Instructions = "Complete a single implementation request and return the result.",
            StartStepName = "act",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "act",
                    Instructions = "Make the requested change, summarize the result, then stop.",
                    WritesMemory = ["result"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "stop", Status = "completed", StopsInvocation = true },
                        new WorkflowTransition { Selection = "ask_user", Status = "blocked", StopsInvocation = true },
                        new WorkflowTransition { Selection = "error", Status = "error", StopsInvocation = true }
                    ]
                }
            ]
        };
    }

    public static WorkflowDefinition RequirementsDefinition()
    {
        return new WorkflowDefinition
        {
            Name = "requirements",
            Instructions = "Clarify requirements, produce tasks, then execute them.",
            StartStepName = "collect_requirements",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "collect_requirements",
                    Instructions = "Clarify the goal.",
                    WritesMemory = ["requirements"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "continue", TargetStepName = "collect_requirements", Status = "active" },
                        new WorkflowTransition { Selection = "to_produce_tasks", TargetStepName = "produce_tasks", Status = "active" },
                        new WorkflowTransition { Selection = "ask_user", Status = "blocked", StopsInvocation = true },
                        new WorkflowTransition { Selection = "error", Status = "error", StopsInvocation = true }
                    ]
                },
                new WorkflowStep
                {
                    Name = "produce_tasks",
                    Instructions = "Break the work into tasks.",
                    ReadsMemory = ["requirements"],
                    WritesMemory = ["tasks"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "continue", TargetStepName = "produce_tasks", Status = "active" },
                        new WorkflowTransition { Selection = "to_execute_tasks", TargetStepName = "execute_tasks", Status = "active" },
                        new WorkflowTransition { Selection = "ask_user", Status = "blocked", StopsInvocation = true },
                        new WorkflowTransition { Selection = "error", Status = "error", StopsInvocation = true }
                    ]
                },
                new WorkflowStep
                {
                    Name = "execute_tasks",
                    Instructions = "Execute the tasks.",
                    ReadsMemory = ["requirements", "tasks"],
                    WritesMemory = ["execution"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "continue", TargetStepName = "execute_tasks", Status = "active" },
                        new WorkflowTransition { Selection = "stop", Status = "completed", StopsInvocation = true },
                        new WorkflowTransition { Selection = "ask_user", Status = "blocked", StopsInvocation = true },
                        new WorkflowTransition { Selection = "error", Status = "error", StopsInvocation = true }
                    ]
                }
            ]
        };
    }

    public static WorkflowDefinition HandoffDefinition()
    {
        return new WorkflowDefinition
        {
            Name = "handoff",
            Instructions = "Test a transition that requires memory handoff.",
            StartStepName = "first",
            Steps =
            [
                new WorkflowStep
                {
                    Name = "first",
                    Instructions = "Write the handoff value.",
                    WritesMemory = ["handoff"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "to_second", TargetStepName = "second", Status = "active" },
                        new WorkflowTransition { Selection = "stop", Status = "completed", StopsInvocation = true }
                    ]
                },
                new WorkflowStep
                {
                    Name = "second",
                    Instructions = "Read the handoff value.",
                    ReadsMemory = ["handoff"],
                    Transitions =
                    [
                        new WorkflowTransition { Selection = "stop", Status = "completed", StopsInvocation = true }
                    ]
                }
            ]
        };
    }
}
