# Stepwise Workflows

Use stepwise mode when you want to inspect or control each routed iteration.

## One iteration at a time

`--step` runs exactly one iteration:

```powershell
wallycode loop "Build a CSV importer." --definition requirements --step --source C:\src\MyRepo --log --verbose
wallycode loop --step --source C:\src\MyRepo --log --verbose
```

`resume --step` is the explicit continuation form:

```powershell
wallycode resume --step --source C:\src\MyRepo --log --verbose
```

## Several iterations in one call

`--steps <n>` runs up to that many iterations, stopping early if the session blocks, completes, or errors:

```powershell
wallycode loop "Review repo structure." --definition requirements --steps 3 --source C:\src\MyRepo --log --verbose
```

## Respond to a blocked session

When the provider selects `[ASK_USER]`, the session is blocked. Save a response, then continue:

```powershell
wallycode respond "Use SQLite and keep the API synchronous for now." --source C:\src\MyRepo --log --verbose
wallycode resume --step --source C:\src\MyRepo --log --verbose
```

The response is included in the next prompt and then cleared from pending responses.

## Useful logging

Use `--log --verbose` while tuning definitions or prompts. Logs include prompt text, raw provider output, selected keyword, next step, and final session status.

## Isolate experiments

An active session owns its workflow definition. Use `--memory-root` to keep experiments separate:

```powershell
wallycode loop "Try an alternate task flow." --definition tasks --step --source C:\src\MyRepo --memory-root C:\temp\wally-tasks
```
