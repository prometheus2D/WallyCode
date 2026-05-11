# Definitions and Steps

Workflow definitions are WallyCode entry points selected by `loop --definition <name>`. Definition names resolve to a start step, and `ask` and `act` are shortcut verbs for two common starts.

## Files

- `WallyCode.Console/Workflow/Steps/*.json` defines reusable shared steps that can be used as workflow starts.
- `WallyCode.Console/Workflow/Transitions/*.json` defines reusable routing transitions that steps opt into with `transitionIds`.

The project file copies workflow step and transition JSON to build and publish output, so edits are available to the console app after a build.

## Built-in definitions

| Definition | Start step | Purpose |
| --- | --- | --- |
| `ask` | `ask` | Answer a question without intending to edit files. |
| `act` | `act` | Complete a file-changing implementation request. |
| `requirements` | `collect_requirements` | Clarify requirements, produce tasks, then execute. This is the default for `loop`. |
| `tasks` | `produce_tasks` | Start at task production, then execute. |
| `full-pipeline` | `collect_requirements` | Run the full requirements-to-execution flow. |

## Run a definition

```powershell
wallycode loop "Build a CSV importer." --definition requirements --source C:\src\MyRepo --log --verbose
wallycode loop "Implement the prepared task list." --definition tasks --source C:\src\MyRepo --log --verbose
wallycode ask "Where is setup implemented?" --source C:\src\MyRepo
wallycode act "Add tests for setup behavior." --source C:\src\MyRepo
```

If an active session exists, it is tied to the workflow definition it started with. Use `--memory-root` for a parallel session with another definition.

## Add or change a definition

1. Add or edit shared step JSON under `WallyCode.Console/Workflow/Steps`.
2. Put step instructions in `instructions`.
3. Use `readsMemory` and `writesMemory` when a step should consume or produce durable session context.
4. Add reusable transitions under `WallyCode.Console/Workflow/Transitions`.
5. Reference those transitions from steps with `transitionIds`.
6. Run `dotnet test WallyCode.sln`.

Example shape:

```json
{
  "id": "collect_requirements",
  "instructions": "Clarify the user's goal, constraints, and expected outcome.",
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

The runner stores memory in `.wallycode/session.json`, injects declared `readsMemory` keys into later prompts, and writes versioned snapshots under `.wallycode/sessions/session-0001.json`, `.wallycode/sessions/session-0002.json`, and so on. Use `null` in the returned `memory` object to remove a key.
