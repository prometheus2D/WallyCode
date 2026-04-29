# Definitions and Steps

Workflow definitions are WallyCode entry points selected by `loop --definition <name>`. `ask` and `act` are shortcut verbs for two common definitions.

## Files

- `WallyCode.Console/Workflow/Definitions/*.json` defines named workflows.
- `WallyCode.Console/Workflow/Steps/*.json` defines reusable shared steps.
- `WallyCode.Console/Workflow/Keywords/*.json` defines shared keyword descriptions.

The project file copies all three folders to build and publish output, so JSON edits are available to the console app after a build.

## Built-in definitions

| Definition | Start step | Purpose |
| --- | --- | --- |
| `ask` | `prompt` | Answer a question without intending to edit files. |
| `act` | `prompt` | Complete a file-changing implementation request. |
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

1. Add or edit a JSON file under `WallyCode.Console/Workflow/Definitions`.
2. Reuse shared steps with `stepRefs`, or define inline `steps` when the behavior is specific to that definition.
3. Give each step an `allowedKeywords` list.
4. Add `transitions` for keywords that move to another step.
5. Keep built-in control keywords predictable: `[CONTINUE]`, `[ASK_USER]`, `[DONE]`, and `[ERROR]`.
6. Run `dotnet test WallyCode.sln`.

Example shape:

```json
{
  "name": "example",
  "startStepName": "collect_requirements",
  "stepRefs": [
    { "ref": "collect_requirements" },
    { "ref": "produce_tasks" }
  ]
}
```

For a shared step, make sure every transition target is a step present in the resolved definition.
