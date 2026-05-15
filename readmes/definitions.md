# Definitions and Steps

Use this guide when you want to customize workflow behavior.

## Prerequisites

Required: run [Setup and providers](setup.md) first for this workspace.

## Inputs

- Workflow definition JSON files.
- Shared step JSON files.
- Shared transition JSON files.

Active folders:
- Loadables/Definitions next to the WallyCode executable you are running.
- Loadables/Steps next to the WallyCode executable you are running.
- Loadables/Transitions next to the WallyCode executable you are running.

After editing Loadables in a source executable location, run `setup --source C:\src\MyRepo --install` or `setup --vs-build --install` to refresh the repo-local payload before testing.

Manual test:
- Use the runnable commands and acceptance criteria below after editing workflow JSON.

## Built-in workflow definitions

| Definition | Start step | Purpose |
| --- | --- | --- |
| ask | ask | One-shot analysis response. |
| act | act | One-shot implementation action. |
| requirements | collect_requirements | Requirements to tasks to execution. Default for run. |
| tasks | produce_tasks | Start at task production and continue execution. |

## Step 1: Run a specific definition

```powershell
.\wallycode.exe run "Build a CSV importer." requirements --source C:\src\MyRepo --max-run-iterations 1 --log --verbose
```

Acceptance criteria:
- Command exits with code 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 2: Add or edit a workflow definition

Create or edit a JSON file in the active `Loadables/Definitions` folder. Save this example as `requirements_custom.json`.

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
- run can select the definition id directly from a clean setup state.

```powershell
.\wallycode.exe run "Validate custom definition." requirements_custom --source C:\src\MyRepo --max-run-iterations 1 --log --verbose
```

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 3: Add or edit shared steps

Create or edit JSON in the active `Loadables/Steps` folder. Save this example as `collect_requirements_custom.json`.

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

Create or edit JSON in the active `Loadables/Transitions` folder. Save this example as `to_produce_tasks_custom.json`.

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
- The minimal verification checklist below exits with code 0, which proves the catalog loaded and validated the new JSON.

## Runtime behavior reference

Provider responses choose selectedStep.

- continue keeps current step.
- selection mapped to a transition moves to targetStepName.
- stop completes the workflow.
- ask_user blocks the session until respond.
- error ends with failure state.

Memory behavior:
- Session memory is replaced after each successful iteration.
- The retained memory is the current non-null response memory plus existing keys declared by the next step's readsMemory.
- Previous memory that is not retained this way is cleared before the next prompt is built.
- Only keys declared in writesMemory are persisted.
- readsMemory keys are injected into later prompts.
- Returning null for a memory key prevents that key from being retained.

Guard behavior:
- Explicit transition guards are evaluated before model-selected transition routing.
- Handoff memory requirements are enforced when target steps depend on keys written by the current step.

## Minimal verification checklist

Run this after any definition/step/transition edit:

```powershell
.\wallycode.exe run "Schema smoke test." requirements --source C:\src\MyRepo --max-run-iterations 1 --log --verbose
.\wallycode.exe status --source C:\src\MyRepo
```

Acceptance criteria:
- Both commands exit with code 0.
- status command prints Session without schema errors.
