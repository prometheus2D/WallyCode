# Setup and Providers

Use this tutorial to prepare one repository for WallyCode.

## Inputs

- Required: target repository path.
- Optional: provider choice and model choice.
- Optional: logging defaults for that repo.

Example values used below:
- Repo path: C:\src\MyRepo
- Provider: gh-copilot-claude
- Model: any model returned by the models command

## Pre-check

Required assertions:
- C:\src\MyRepo exists.

```powershell
Test-Path C:\src\MyRepo
```

## Step 1: Initialize repo settings

```powershell
wallycode setup --directory C:\src\MyRepo
```

Required assertions:
- Exit code is 0.
- C:\src\MyRepo\wallycode.json exists.
- C:\src\MyRepo\.wallycode exists.

```powershell
Test-Path C:\src\MyRepo\wallycode.json
Test-Path C:\src\MyRepo\.wallycode
```

If launched from a Visual Studio build output folder, use:

```powershell
wallycode setup --vs-build
```

Use force only when you want to regenerate defaults:

```powershell
wallycode setup --directory C:\src\MyRepo --force
```

Expected outcome:
- Recreates setup artifacts with default values.

## Step 2: List providers and readiness

```powershell
wallycode provider --source C:\src\MyRepo
```

Required assertions:
- Exit code is 0.
- Output includes provider names and readiness status lines.

Built-in providers:
- gh-copilot-claude
- gh-copilot-gpt

## Step 3: Set default provider

```powershell
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
```

Required assertions:
- Exit code is 0.
- wallycode.json property provider equals gh-copilot-claude.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).provider -eq 'gh-copilot-claude')
```

## Step 4: List models and set default model

```powershell
wallycode provider gh-copilot-claude --models --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model <model-from-previous-list> --source C:\src\MyRepo
```

Required assertions:
- Both commands exit with code 0.
- wallycode.json property model equals the selected model.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).model -eq '<model-from-previous-list>')
```

## Step 5: Optional workspace logging defaults

Enable default logging for future commands:

```powershell
wallycode logging --enable --verbose --source C:\src\MyRepo
```

Required assertions:
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
wallycode logging --disable --quiet --source C:\src\MyRepo
```

Required assertions:
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
wallycode status --source C:\src\MyRepo
```

Required assertions:
- Exit code is 0.
- Output contains Source:, Memory root:, Provider:, and Model:.
- Output contains Session: (none) when no session exists.

Follow-up behavior:
- Future run/ask/act/resume/respond/recover/step commands can omit `--source` and still reuse persisted defaults from wallycode.json when invoked from this workspace.
