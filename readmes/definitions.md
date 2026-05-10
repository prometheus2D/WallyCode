# Definitions and Steps

Workflow definitions are WallyCode entry points selected by `loop --definition <name>`. `ask` and `act` are shortcut verbs for two common definitions.

## Files

- `WallyCode.Console/Workflow/Definitions/*.json` defines named workflows.
- `WallyCode.Console/Workflow/Steps/*.json` defines reusable shared steps.

The project file copies workflow step JSON to build and publish output, so edits are available to the console app after a build.

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
3. Put direct step routes in `transitions`. Each JSON transition must set `targetStepName` to another step defined by a loadable JSON file.
4. Run `dotnet test WallyCode.sln`.

Example shape:

```json
{
  "id": "collect_requirements",
  "instructions": "Clarify the user's goal, constraints, and expected outcome.",
  "transitions": [
    {
      "selection": "produce_tasks",
      "description": "Requirements are clear enough to break the work into tasks.",
      "targetStepName": "produce_tasks"
    }
  ]
}
```

At runtime, the provider returns `selectedStep`. Selecting the current step continues, selecting a JSON-defined transition moves to its `targetStepName`, `ask_user` blocks for `respond`, `done` completes, and `error` stops with the summary as the reason. `ask_user`, `done`, and `error` are built-in terminal outcomes, not JSON transitions.
