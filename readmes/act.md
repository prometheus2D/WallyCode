# Act Workflow

Use act for implementation goals where file changes are expected.

act is equivalent to running run with workflow act.

## Inputs

- Required to start a new session: prompt text with desired change.
- Optional: source path.
- Optional: memory-root for isolated session state.
- Optional: provider or model override.
- Optional: max-run-iterations, max-total-iterations, max-step-repeats.

## Step 1: Start an implementation session

```powershell
wallycode act "Add a setup tutorial README." --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Starts or continues an act workflow session.
- Workflow can edit files during implementation steps.

## Step 2: Continue until completion or block

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Continues active act session.
- Stops if workflow reaches stop, ask_user, error, or iteration limits.

## Step 3: Respond if blocked

```powershell
wallycode respond "Use the existing command option style." --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Response is stored and the session resumes automatically.

## Optional: allow more work per invocation

```powershell
wallycode act "Fix these code problems: <paste problems here>" --source C:\src\MyRepo --max-run-iterations 40 --log --verbose
```

Expected outcome:
- One command can run more workflow iterations before returning.

## Optional: local source-build usage

```powershell
dotnet run --project WallyCode.Console -- act "Update development-mode documentation." --source . --memory-root .wallycode-dev --log --verbose
```

Expected outcome:
- Runs act with the local source build against the current repo.
