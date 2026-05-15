# Setup and Providers

Use this tutorial to initialize one repository or new working folder for WallyCode.

## Prerequisites

- A terminal is open in the folder that contains `wallycode.exe`.
- Replace `C:\src\MyRepo` with a disposable repo or working folder when running this as a user test.

## Inputs

- Required: target repository or working-folder path.
- Optional: provider choice and model choice.
- Optional: logging defaults for that repo.
- Optional: deploy a local WallyCode executable into the target folder.

Example values used below:
- Repo path: C:\src\MyRepo
- Provider: gh-copilot-claude
- Model: claude-haiku-4.5

## Pre-check

Acceptance criteria:
- The target path is valid for the local machine.
- The drive or root location exists.
- The target folder may already exist, but setup can create it if it is missing.

## Step 1: Initialize repo settings

```powershell
.\wallycode.exe setup --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo exists.
- C:\src\MyRepo\wallycode.json exists.
- C:\src\MyRepo\.wallycode exists.
- wallycode.active.json next to the exe points to C:\src\MyRepo.

```powershell
Test-Path C:\src\MyRepo
Test-Path C:\src\MyRepo\wallycode.json
Test-Path C:\src\MyRepo\.wallycode
((Get-Content .\wallycode.active.json -Raw | ConvertFrom-Json).activeProjectPath -eq 'C:\src\MyRepo')
```

If launched from a Visual Studio build output folder, use:

```powershell
.\wallycode.exe setup --vs-build
```

Expected behavior for --vs-build:
- Command must be launched from a path under bin\Debug or bin\Release.
- WallyCode resolves the workspace root above that output folder.
- setup artifacts are created at the resolved workspace root, not in the output folder.
- wallycode.active.json points to the resolved workspace root.

## Step 1b: Optional deploy local executable

Use either command. `deploy` is equivalent to `setup --deploy` with the same supported setup flags.

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --deploy
.\wallycode.exe deploy --source C:\src\MyRepo
```

Expected outcome:
- Exit code is 0.
- Output says deployment was successful and shows the new executable path.
- C:\src\MyRepo\wallycode.exe exists.
- C:\src\MyRepo\wallycode.active.json points to C:\src\MyRepo.
- Future commands can run from C:\src\MyRepo with the local `wallycode.exe`.

```powershell
Test-Path C:\src\MyRepo\wallycode.exe
((Get-Content C:\src\MyRepo\wallycode.active.json -Raw | ConvertFrom-Json).activeProjectPath -eq 'C:\src\MyRepo')
```

After deployment, switch to the source folder when using the local executable:

```powershell
Set-Location C:\src\MyRepo
.\wallycode.exe status
```

## Step 1c: Optional cleanup + regenerate

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --cleanup
```

Expected outcome:
- Removes existing wallycode.json and .wallycode first.
- Recreates setup artifacts with default values.
- Updates wallycode.active.json to point at C:\src\MyRepo.
- Keeps any deployed local WallyCode executable payload in place. Use `cleanup` to remove the deployed local executable.

## Step 1d: Remove setup artifacts cleanly

```powershell
.\wallycode.exe cleanup --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\wallycode.json does not exist.
- C:\src\MyRepo\.wallycode does not exist.
- If the source had a deployed local executable, C:\src\MyRepo\wallycode.exe and C:\src\MyRepo\Loadables do not exist.
- If the source had a deployed local executable, C:\src\MyRepo\wallycode.active.json does not exist.
- If C:\src\MyRepo was active, wallycode.active.json is removed.

```powershell
Test-Path C:\src\MyRepo\wallycode.json
Test-Path C:\src\MyRepo\.wallycode
Test-Path C:\src\MyRepo\wallycode.exe
Test-Path C:\src\MyRepo\Loadables
Test-Path C:\src\MyRepo\wallycode.active.json
```

Expected output for all checks is `False`.

Recreate setup state after cleanup:

```powershell
.\wallycode.exe setup --source C:\src\MyRepo
```

## Step 2: List providers and readiness

```powershell
.\wallycode.exe provider
```

Acceptance criteria:
- Exit code is 0.
- Output includes provider names and readiness status lines.

Built-in providers:
- gh-copilot-claude
- gh-copilot-gpt

## Step 3: Set default provider

```powershell
.\wallycode.exe provider gh-copilot-claude --set --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- wallycode.json property provider equals gh-copilot-claude.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).provider -eq 'gh-copilot-claude')
```

## Step 4: List models and set default model

```powershell
.\wallycode.exe provider gh-copilot-claude --models --source C:\src\MyRepo
.\wallycode.exe provider gh-copilot-claude --model claude-haiku-4.5 --source C:\src\MyRepo
```

Acceptance criteria:
- Both commands exit with code 0.
- wallycode.json property model equals the selected model.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).model -eq 'claude-haiku-4.5')
```

## Step 5: Optional workspace logging defaults

Enable default logging for future commands:

```powershell
.\wallycode.exe logging --enable --verbose --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- wallycode.json logging.enabled is True.
- wallycode.json logging.verbose is True.

```powershell
$settings = Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json
($settings.logging.enabled -eq $true)
($settings.logging.verbose -eq $true)
```

Disable later if needed:

```powershell
.\wallycode.exe logging --disable --quiet --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- wallycode.json logging.enabled is False.
- wallycode.json logging.verbose is False.

```powershell
$settings = Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json
($settings.logging.enabled -eq $false)
($settings.logging.verbose -eq $false)
```

## Step 6: Verify setup

```powershell
.\wallycode.exe status --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- Output contains Source:, Provider:, and Model:.
- Output contains Session: (none) when no session exists.

Follow-up behavior:
- run/ask/act/step require setup artifacts in the target workspace.
- provider/logging/status/shell also require initialized setup state.
- respond/resume/recover require an existing workspace session.

## Step 7: Use workflows immediately

After setup writes the active source pointer, these commands can run from the exe folder without repeating `--source`. If you used deployment, run these from C:\src\MyRepo with the deployed `wallycode.exe`.

Use `ask` for a one-shot question:

```powershell
.\wallycode.exe ask "What does this project do?"
```

Use `act` for a one-shot implementation action:

```powershell
.\wallycode.exe act "Add a short README section describing how to run the project."
```

Use `run` for the workflow loop. The default `requirements` workflow collects requirements, produces tasks, and executes tasks:

```powershell
.\wallycode.exe run "Build a simple browser Tic Tac Toe game with a README." requirements --log --verbose
```

If the session blocks:

```powershell
.\wallycode.exe respond "Proceed with the simplest browser-only implementation."
```

If the session is still active:

```powershell
.\wallycode.exe resume
```

If requirements are already clear and you want to start at task creation:

```powershell
.\wallycode.exe run "Create tasks for adding score tracking, then implement them." tasks --log --verbose
```

Acceptance criteria:
- Each command exits with code 0.
- Workflow commands create or update a session under C:\src\MyRepo\.wallycode.
- If a workflow blocks, `respond` exits with code 0 and resumes the session.
