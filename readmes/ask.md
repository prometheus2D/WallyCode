# Ask Workflow

Use ask for analysis-only goals.

ask is equivalent to running run with workflow ask.

## Prerequisites

Recommended: run [Setup and providers](setup.md) first for stable defaults.

If setup is skipped:
- ask still runs.
- .wallycode session state is created lazily.
- wallycode.json is created only when a command persists settings.

## Inputs

- Required to start a new session: prompt text.
- Optional: source path.
- Optional: memory-root for isolated session state.
- Optional: provider or model override.
- Optional: max-run-iterations, max-total-iterations, max-step-repeats.

Example values used below:
- Repo path: C:\src\MyRepo
- Isolated memory root: C:\temp\wally-ask

Tutorial test:
- AskTutorialTests.Ask_flow_persists_session_and_memory

## Step 1: Ask a question

```powershell
wallycode ask "What does this repository do?" --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 2: Continue if still active

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json still exists.

## Step 3: Respond if blocked

```powershell
wallycode respond "Focus on command handlers and workflow transitions." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- If the session is blocked, exit code is 0 and session continues.
- If the session is not blocked, command should explain that no blocked session is waiting.

## Optional: isolate analysis sessions

```powershell
wallycode ask "Trace the setup flow." --source C:\src\MyRepo --memory-root C:\temp\wally-ask --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\temp\wally-ask\session.json exists.

```powershell
Test-Path C:\temp\wally-ask\session.json
```

## Optional: local source-build usage

```powershell
dotnet run --project WallyCode.Console -- ask "Summarize the command handlers." --source . --memory-root .wallycode-dev --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- .wallycode-dev\session.json exists in the current repository.

```powershell
Test-Path .\.wallycode-dev\session.json
```

