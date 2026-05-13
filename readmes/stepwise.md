# Stepwise Workflows

Use this tutorial when you want deliberate control over workflow progress.

## Prerequisites

Required: run [Setup and providers](setup.md) first for this workspace.

## Inputs

- Prompt to start session.
- Optional workflow name.
- Optional source and memory-root.
- Optional iteration controls.

Example values used below:
- Repo path: C:\src\MyRepo
- Isolated memory root: C:\temp\wally-tasks

Tutorial test:
- StepwiseTutorialTests.Requirements_flow_persists_snapshots_across_iterations

## Step 1: Start a session

```powershell
wallycode run "Build a CSV importer." requirements --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 2: Continue the same session

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\sessions exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\sessions
```

## Step 3: Bound work per invocation

```powershell
wallycode run "Review repo structure." requirements --max-run-iterations 3 --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- Session remains valid and readable through status command.

```powershell
wallycode status --source C:\src\MyRepo
```

## Step 4: Handle blocked sessions

```powershell
wallycode respond "Use SQLite and keep the API synchronous for now." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- If blocked, exit code is 0 and run resumes.
- If not blocked, command explains no blocked session is waiting.

## Step 4b: Recover from terminal error state

```powershell
wallycode recover "Retry with a narrower scope and keep existing routing." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Command is valid only when session status is terminal.
- On success, C:\src\MyRepo\.wallycode\archive exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\archive
```

## Step 5: Run a direct shared step

```powershell
wallycode step "Review the current workspace changes." review_changes --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- Command completes even if no workflow session is active.

## Step 6: Isolate experiments

```powershell
wallycode run "Try an alternate task flow." tasks --source C:\src\MyRepo --memory-root C:\temp\wally-tasks --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\temp\wally-tasks\session.json exists.

```powershell
Test-Path C:\temp\wally-tasks\session.json
```

## Logging reference

With log and verbose enabled:
- Prompts and raw provider output are logged.
- Selected transition and next step are logged.
- Session snapshots are written to sessions/session-000N.json.
