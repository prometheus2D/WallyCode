# Stepwise Workflows

Use stepwise mode when you want to inspect or control each routed iteration.

## One workflow iteration at a time

`run` and `resume` run one workflow iteration by default:

```powershell
wallycode run "Build a CSV importer." requirements --source C:\src\MyRepo --log --verbose
wallycode resume --source C:\src\MyRepo --log --verbose
```

Use `step` when you want to run one shared step directly without advancing a workflow session:

```powershell
wallycode step "Review the current workspace changes." review_changes --source C:\src\MyRepo --log --verbose
```

## Several iterations in one call

`--max-iterations <n>` sets the largest number of iterations to run, stopping early if the session blocks, completes, or errors:

```powershell
wallycode run "Review repo structure." requirements --max-iterations 3 --source C:\src\MyRepo --log --verbose
```

## Respond to a blocked session

When the provider selects `ask_user`, the session is blocked. `respond` saves the answer and immediately resumes the workflow:

```powershell
wallycode respond "Use SQLite and keep the API synchronous for now." --source C:\src\MyRepo --log --verbose
```

The response is included in the next prompt and then cleared from pending responses.

## Useful logging

Use `--log --verbose` while tuning definitions or prompts. Logs include prompt text, raw provider output, selected step, next step, and final session status.

Each completed iteration also writes the current session state to `sessions/session-000N.json` under the runtime root. The active `session.json` remains the latest state used by the next command.

The workflow engine is orchestrated: the active step is executed by a step executor, explicit guards and derived handoff requirements are checked, memory updates are filtered through `writesMemory`, and then the latest session plus a versioned snapshot are saved.

## Isolate experiments

An active session owns its workflow definition. Use `--memory-root` to keep experiments separate:

```powershell
wallycode run "Try an alternate task flow." tasks --source C:\src\MyRepo --memory-root C:\temp\wally-tasks
```

By default WallyCode keeps running bounded iterations until a step selects `stop`, `ask_user`, `error`, or the max iteration limit is reached:

```powershell
wallycode act "Fix these code problems: <paste problems here>" --source C:\src\MyRepo --log --verbose
```
