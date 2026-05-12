# Act Workflow

Use `act` when WallyCode should complete an implementation-oriented request and may change files.

`act` is a shortcut for:

```powershell
wallycode run "..." act
```

The `act` definition starts at the `act` step, which may change files. When implementation changes are ready, it writes `implementation` memory and moves to `review_changes`. The review step checks the workspace against the goal and either selects `stop`, asks for input with `ask_user`, continues reviewing, or writes `review` feedback and routes back to `act` for another pass.

## Basic usage

```powershell
wallycode act "Add a setup tutorial README." --source C:\src\MyRepo --log --verbose
```

For larger fix work, let the orchestrator run bounded iterations and stop early when the review step selects `stop`:

```powershell
wallycode act "Fix these code problems: <paste problems here>" --source C:\src\MyRepo --log --verbose
```

The default max run iteration limit is 20 per invocation. If the limit is reached before completion, WallyCode leaves the session active so you can continue with `resume` or raise the limit with `--max-run-iterations`.

From the WallyCode source tree while developing WallyCode itself:

```powershell
dotnet run --project WallyCode.Console -- act "Update the development-mode documentation." --source . --memory-root .wallycode-dev --log --verbose
```

## Before running act

- Make sure the target repo is the one you expect with `--source`.
- Use `--memory-root` if another session is already active.
- Prefer `ask` first when the request needs investigation but not edits yet.
- Start from a known git state so the resulting diff is easy to review.

## After running act

Inspect the diff and run the relevant validation command. For WallyCode itself, the usual check is:

```powershell
dotnet build WallyCode.sln
```

If the active session is blocked, answer it and continue automatically:

```powershell
wallycode respond "Use the existing command option style." --source C:\src\MyRepo
```
