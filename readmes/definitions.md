# Definitions and Steps

Workflow definitions are WallyCode entry points selected by `loop --definition <name>`. A definition owns workflow-level instructions, aliases, a start step, and the set of shared steps allowed in that workflow. `stepIds` defines the route surface for that workflow: transitions to targets outside the set are removed from the compiled workflow before the provider prompt and resolver see them. `ask` and `act` are shortcut verbs for two common definitions.

## Files

- `WallyCode.Console/Workflow/Definitions/*.json` defines named workflows: instructions, aliases, start step, and allowed step IDs.
- `WallyCode.Console/Workflow/Steps/*.json` defines reusable shared steps.
- `WallyCode.Console/Workflow/Transitions/*.json` defines reusable routing transitions that steps opt into with `transitionIds`.

The project file copies workflow definition, step, and transition JSON to build and publish output, so edits are available to the console app after a build.

## Built-in definitions

| Definition | Start step | Purpose |
| --- | --- | --- |
| `ask` | `ask` | Answer a question without intending to edit files. |
| `act` | `act` | Complete a file-changing implementation request through an implementation/review loop. |
| `requirements` | `collect_requirements` | Clarify requirements, produce tasks, then execute. This is the default for `loop`. Aliases: `collect_requirements`, `full-pipeline`. |
| `tasks` | `produce_tasks` | Start at task production, then execute. |

## Run a definition

```powershell
wallycode loop "Build a CSV importer." --definition requirements --source C:\src\MyRepo --log --verbose
wallycode loop "Implement the prepared task list." --definition tasks --source C:\src\MyRepo --log --verbose
wallycode ask "Where is setup implemented?" --source C:\src\MyRepo
wallycode act "Update setup behavior." --source C:\src\MyRepo
wallycode act "Fix these code problems: ..." --until-complete --source C:\src\MyRepo
```

If an active session exists, it is tied to the workflow definition it started with. Use `--memory-root` for a parallel session with another definition.

## Add or change a definition

1. Add or edit a workflow JSON file under `WallyCode.Console/Workflow/Definitions`.
2. Put workflow-level instructions in `instructions`.
3. Choose `startStepName` and the allowed `stepIds`. Only transitions targeting those steps are available in that workflow.
4. Add or edit shared step JSON under `WallyCode.Console/Workflow/Steps`.
5. Use step `readsMemory` and `writesMemory` when a step should consume or produce durable session context.
6. Add reusable transitions under `WallyCode.Console/Workflow/Transitions` and reference them from steps with `transitionIds`.
7. Run `dotnet build WallyCode.sln`.

Example workflow definition:

```json
{
  "id": "requirements",
  "aliases": ["collect_requirements", "full-pipeline"],
  "instructions": "Clarify requirements, produce tasks, execute the tasks, and finish only when the requested outcome is complete.",
  "startStepName": "collect_requirements",
  "stepIds": ["collect_requirements", "produce_tasks", "execute_tasks"]
}
```

Example shared step:

```json
{
  "id": "collect_requirements",
  "executionKind": "provider",
  "instructions": "Clarify the user's goal, constraints, and expected outcome. When ready, write requirements.",
  "writesMemory": ["requirements"],
  "transitionIds": ["continue", "to_produce_tasks"]
}
```

Example transition:

```json
{
  "id": "to_produce_tasks",
  "selection": "produce_tasks",
  "description": "Requirements are clear enough to break the work into tasks.",
  "targetStepName": "produce_tasks"
}
```

At runtime, the provider returns `selectedStep`. Selecting `continue` stays on the current step, selecting a route like `produce_tasks` moves to that transition's `targetStepName`, selecting `stop` completes the workflow, `ask_user` blocks for `respond`, and `error` stops with the summary as the reason. `done` is still accepted as a compatibility alias for completion, but new transitions should use `stop`.

Steps can also update session memory by returning a top-level `memory` object:

```json
{
  "selectedStep": "produce_tasks",
  "summary": "Requirements are ready.",
  "memory": {
    "requirements": "Import comma-separated files and reject invalid rows."
  }
}
```

The orchestrator stores memory in `.wallycode/session.json`, injects declared `readsMemory` keys into later prompts, and writes versioned snapshots under `.wallycode/sessions/session-0001.json`, `.wallycode/sessions/session-0002.json`, and so on. Use `null` in the returned `memory` object to remove a key.

The orchestrator filters memory updates through the active step's `writesMemory` list. Undeclared memory keys are ignored so one step cannot accidentally overwrite another step's durable context.

When a transition targets another step, the resolver derives simple handoff requirements from memory contracts. If the current step can write a key and the target step reads that same key, the key must exist before the transition can move forward. For example, `collect_requirements` writes `requirements`, `produce_tasks` reads `requirements`, so selecting `produce_tasks` requires the provider to write `memory.requirements` in the same response or have it already stored in the session.

Transitions can also define explicit guards for advanced deterministic routing. Guarded transitions are evaluated before the model-selected transition, and a guarded route cannot be selected directly unless its guard matches:

```json
{
  "selection": "review_result",
  "targetStepName": "review_result",
  "guard": {
    "memoryEquals": {
      "validation.status": "passed"
    }
  }
}
```

Use guarded transitions when the route should follow verified session state instead of another model judgment.
