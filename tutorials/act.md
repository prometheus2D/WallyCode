# Act Workflow

Use act for one-shot implementation goals where file changes are expected.

act is equivalent to running run with workflow act.

## Prerequisites

Required: run [Setup and providers](setup.md) first for this workspace.

## Inputs

- Required to start a new session: prompt text with desired change.
- Optional: source path.
- Optional: provider or model override.
- Optional: max-run-iterations, max-total-iterations, max-step-repeats.

Example values used below:
- Repo path: C:\src\MyRepo

Tutorial test:
- UserWorkflowCommandTests.AskAndActAliasesUseMockProviderThroughUserCommandPath

## Step 1: Start an implementation action

```powershell
.\wallycode.exe act "Add a setup tutorial README." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 2: Respond if blocked

```powershell
.\wallycode.exe respond "Use the existing command option style." --source C:\src\MyRepo --log --verbose
```

Acceptance criteria:
- If blocked, exit code is 0 and run continues.
- If not blocked, command explains no blocked session is waiting.

## Optional: local source-build usage

```powershell
dotnet run --project WallyCode.Console -- act "Update development-mode documentation." --source . --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- .wallycode\session.json exists in the current repository.

```powershell
Test-Path .\.wallycode\session.json
```
