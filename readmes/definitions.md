# Definitions and Steps

Use this guide when you want to customize workflow behavior.

## Inputs

- Workflow definition JSON files.
- Shared step JSON files.
- Shared transition JSON files.

Key folders:
- WallyCode.Console/Workflow/Definitions
- WallyCode.Console/Workflow/Steps
- WallyCode.Console/Workflow/Transitions

## Built-in workflow definitions

| Definition | Start step | Purpose |
| --- | --- | --- |
| ask | ask | Analysis-oriented responses. |
| act | act | Implementation and review loop. |
| requirements | collect_requirements | Requirements to tasks to execution. Default for run. |
| tasks | produce_tasks | Start at task production and continue execution. |

## Step 1: Run a specific definition

```powershell
wallycode run "Build a CSV importer." requirements --source C:\src\MyRepo --log --verbose
wallycode run "Implement prepared tasks." tasks --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Starts or continues a session bound to the chosen definition.

## Step 2: Add or edit a workflow definition

Create or edit a JSON file in WallyCode.Console/Workflow/Definitions.

Example:

```json
{
  "id": "requirements",
  "instructions": "Clarify requirements, produce tasks, execute tasks, and finish only when the outcome is complete.",
  "startStepName": "collect_requirements",
  "stepIds": ["collect_requirements", "produce_tasks", "execute_tasks"]
}
```

Expected outcome:
- Definition can be selected by run using its id.

## Step 3: Add or edit shared steps

Create or edit JSON in WallyCode.Console/Workflow/Steps.

Example:

```json
{
  "id": "collect_requirements",
  "executionKind": "provider",
  "instructions": "Clarify goal, constraints, and expected outcome. When ready, write requirements.",
  "writesMemory": ["requirements"],
  "transitionIds": ["continue", "to_produce_tasks"]
}
```

Expected outcome:
- Step becomes available to definitions that include its id in stepIds.

## Step 4: Add or edit transitions

Create or edit JSON in WallyCode.Console/Workflow/Transitions.

Example:

```json
{
  "id": "to_produce_tasks",
  "selection": "produce_tasks",
  "description": "Requirements are clear enough to break the work into tasks.",
  "targetStepName": "produce_tasks"
}
```

Expected outcome:
- Transition can be selected by steps that reference it in transitionIds.

## Runtime behavior reference

Provider responses choose selectedStep.

- continue keeps current step.
- selection mapped to a transition moves to targetStepName.
- stop completes the workflow.
- ask_user blocks the session until respond.
- error ends with failure state.

Memory behavior:
- Response memory is merged into session memory.
- Only keys declared in writesMemory are persisted.
- readsMemory keys are injected into later prompts.
- Returning null for a memory key removes it.

Guard behavior:
- Explicit transition guards are evaluated before model-selected transition routing.
- Handoff memory requirements are enforced when target steps depend on keys written by the current step.
