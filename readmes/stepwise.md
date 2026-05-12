# Stepwise Workflows

Use this tutorial when you want deliberate control over workflow progress.

## Inputs

- Prompt to start session.
- Optional workflow name.
- Optional source and memory-root.
- Optional iteration controls.

## Step 1: Start a session

```powershell
wallycode run "Build a CSV importer." requirements --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Creates or continues a requirements session.

## Step 2: Continue the same session

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Advances the active session further.

## Step 3: Bound work per invocation

```powershell
wallycode run "Review repo structure." requirements --max-run-iterations 3 --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Runs up to 3 iterations in one invocation.
- Returns earlier if workflow stops, blocks, or errors.

## Step 4: Handle blocked sessions

```powershell
wallycode respond "Use SQLite and keep the API synchronous for now." --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Stores response and resumes automatically.

## Step 4b: Recover from terminal error state

```powershell
wallycode recover "Retry with a narrower scope and keep existing routing." --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Archives the terminal session.
- Starts a new run on the same workflow/provider/model with your recovery text.

## Step 5: Run a direct shared step

```powershell
wallycode step "Review the current workspace changes." review_changes --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Executes one shared step directly.
- Does not advance a durable workflow session.

## Step 6: Isolate experiments

```powershell
wallycode run "Try an alternate task flow." tasks --source C:\src\MyRepo --memory-root C:\temp\wally-tasks --log --verbose
```

Expected outcome:
- Uses separate session/runtime files under the alternate memory root.

## Logging reference

With log and verbose enabled:
- Prompts and raw provider output are logged.
- Selected transition and next step are logged.
- Session snapshots are written to sessions/session-000N.json.
```
