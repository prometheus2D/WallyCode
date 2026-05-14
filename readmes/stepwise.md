# Stepwise Workflow Loop

Use this tutorial when you want deliberate control over the multi-step workflow loop.

The default `requirements` workflow moves through three work phases:
- `collect_requirements`: clarify the goal and constraints.
- `produce_tasks`: turn requirements into concrete tasks.
- `execute_tasks`: perform the planned work and finish when complete.

The `tasks` workflow starts at task creation when requirements are already clear.

## Prerequisites

Required: run [Setup and providers](setup.md) first for this workspace.

## Inputs

- Prompt to start session.
- Optional workflow name.
- Optional source path.
- Optional iteration controls.

Example values used below:
- Repo path: C:\src\MyRepo

Tutorial test:
- UserWorkflowCommandTests.RequirementsWorkflowLoopsThroughRequirementsTasksAndExecutionWithMockProvider

## Step 1: Start a session

```powershell
.\wallycode.exe run "Build a CSV importer." requirements --source C:\src\MyRepo --max-run-iterations 1 --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 2: Continue the same session

```powershell
.\wallycode.exe resume --source C:\src\MyRepo --max-run-iterations 1 --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\sessions exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\sessions
```

## Step 3: Bound work per invocation

Continue the current session with a larger per-invocation limit:

```powershell
.\wallycode.exe resume --max-run-iterations 3 --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- Session remains valid and readable through status command.

```powershell
.\wallycode.exe status --source C:\src\MyRepo
```

## Step 4: Optional blocked-session response

Run this only when `status` shows the session is blocked:

```powershell
.\wallycode.exe respond "Use SQLite and keep the API synchronous for now." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- If blocked, exit code is 0 and run resumes.
- If not blocked, command explains no blocked session is waiting.

## Step 4b: Optional terminal-session recovery

Run this only when `status` shows the session is completed or failed:

```powershell
.\wallycode.exe recover "Retry with a narrower scope and keep existing routing." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Command is valid only when session status is completed or failed.
- On success, C:\src\MyRepo\.wallycode\archive exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\archive
```

## Step 5: Run a direct shared step

```powershell
.\wallycode.exe step "Review the current workspace changes." review_changes --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- Command completes even if no workflow session is active.

## Step 6: Start from task planning

```powershell
.\wallycode.exe run "Try an alternate task flow." tasks --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Logging reference

With log and verbose enabled:
- Prompts and raw provider output are logged.
- Selected transition and next step are logged.
- Session snapshots are written to sessions/session-000N.json.
