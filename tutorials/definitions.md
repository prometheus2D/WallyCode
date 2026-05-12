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

Tutorial test:
- DefinitionsTutorialTests.Workflow_definition_json_validates_and_catalog_compiles_from_workspace_files
- TransitionsTutorialTests.Handoff_memory_routes_to_next_step

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

Acceptance criteria:
- Both commands exit with code 0.
- status output shows Session with chosen workflow name when active.

## Step 2: Add or edit a workflow definition

Create or edit a JSON file in WallyCode.Console/Workflow/Definitions.

Example:

```json
{
  "id": "requirements_custom",
  "instructions": "Clarify requirements, produce tasks, execute tasks, and stop only when complete.",
  "startStepName": "collect_requirements",
  "stepIds": ["collect_requirements", "produce_tasks", "execute_tasks"]
}
```

Acceptance criteria:
- JSON file is valid.
- run can select the definition id directly.

```powershell
wallycode run "Validate custom definition." requirements_custom --source C:\src\MyRepo --log --verbose
```

## Step 3: Add or edit shared steps

Create or edit JSON in WallyCode.Console/Workflow/Steps.

Example:

```json
{
  "id": "collect_requirements_custom",
  "executionKind": "provider",
  "instructions": "Clarify goal, constraints, and expected outcome. When ready, write requirements.",
  "writesMemory": ["requirements"],
  "transitionIds": ["continue", "to_produce_tasks"]
}
```

Acceptance criteria:
- JSON file is valid.
- Any definition referencing this step id can run.

## Step 4: Add or edit transitions

Create or edit JSON in WallyCode.Console/Workflow/Transitions.

Example:

```json
{
  "id": "to_produce_tasks_custom",
  "selection": "produce_tasks",
  "description": "Requirements are clear enough to break the work into tasks.",
  "targetStepName": "produce_tasks"
}
```

Acceptance criteria:
- JSON file is valid.
- Steps referencing this transition id can route to targetStepName.

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

## Minimal verification checklist

Run this after any definition/step/transition edit:

```powershell
wallycode run "Schema smoke test." requirements --source C:\src\MyRepo --max-run-iterations 1 --log --verbose
wallycode status --source C:\src\MyRepo
```

Acceptance criteria:
- Both commands exit with code 0.
- status command prints Session without schema errors.

