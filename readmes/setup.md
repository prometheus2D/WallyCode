# Setup and Providers

Use this tutorial to initialize one repository or new working folder for WallyCode.

## Inputs

- Required: target repository or working-folder path.
- Optional: provider choice and model choice.
- Optional: logging defaults for that repo.

Example values used below:
- Repo path: C:\src\MyRepo
- Provider: gh-copilot-claude
- Model: any model returned by the models command

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

## Step 1b: Optional cleanup + regenerate

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --cleanup
```

Expected outcome:
- Removes existing wallycode.json and .wallycode first.
- Recreates setup artifacts with default values.
- Updates wallycode.active.json to point at C:\src\MyRepo.

## Step 1c: Remove setup artifacts cleanly

```powershell
.\wallycode.exe cleanup --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\wallycode.json does not exist.
- C:\src\MyRepo\.wallycode does not exist.
- If C:\src\MyRepo was active, wallycode.active.json is removed.

```powershell
Test-Path C:\src\MyRepo\wallycode.json
Test-Path C:\src\MyRepo\.wallycode
```

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
.\wallycode.exe provider gh-copilot-claude --model <model-from-previous-list> --source C:\src\MyRepo
```

Acceptance criteria:
- Both commands exit with code 0.
- wallycode.json property model equals the selected model.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).model -eq '<model-from-previous-list>')
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
- Output contains Source:, Memory root:, Provider:, and Model:.
- Output contains Session: (none) when no session exists.

Follow-up behavior:
- run/ask/act/step require setup artifacts in the target workspace.
- provider/logging/status/shell also require initialized setup state.
- respond/resume/recover require an existing session at the selected memory root.
