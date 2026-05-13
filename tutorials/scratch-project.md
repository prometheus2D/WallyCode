# Scratch Project From A New Folder

Use this tutorial to point WallyCode at a new folder and have it create an initial solution or program from scratch.

This flow relies on the active project pointer. Run `.\wallycode.exe` from the folder that contains the exe. After setup writes `wallycode.active.json` next to the exe, later commands can keep running from that exe folder without repeating `--source`.

## Prerequisites

Required:
- A terminal is open in the folder that contains `wallycode.exe`.
- Provider setup is complete, or you are ready to set provider/model in this tutorial.
- Any SDK requested in the prompt is installed. For the example below, install the .NET SDK.

## Inputs

- Required: target folder path.
- Required: prompt describing the project to create.
- Optional: provider and model choice.

Example values used below:
- Scratch folder: C:\src\ScratchTodo
- Provider: gh-copilot-claude
- Model: any model returned by the models command

## Step 1: Initialize the new folder

```powershell
.\wallycode.exe setup --source C:\src\ScratchTodo
```

Acceptance criteria:
- Exit code is 0.
- C:\src\ScratchTodo exists, even if it did not exist before setup.
- C:\src\ScratchTodo\wallycode.json exists.
- C:\src\ScratchTodo\.wallycode exists.
- `wallycode.active.json` next to the exe points to C:\src\ScratchTodo.

```powershell
Test-Path C:\src\ScratchTodo
Test-Path C:\src\ScratchTodo\wallycode.json
Test-Path C:\src\ScratchTodo\.wallycode
```

## Step 2: Confirm the active source

Run this from the exe folder:

```powershell
.\wallycode.exe status
```

Acceptance criteria:
- Exit code is 0.
- Output shows C:\src\ScratchTodo as the active source.
- Output shows the session root under C:\src\ScratchTodo\.wallycode unless a custom memory root is configured.

## Step 3: Set provider and model

These commands use the active source and do not need `--source`:

```powershell
.\wallycode.exe provider gh-copilot-claude --set
.\wallycode.exe provider gh-copilot-claude --models
.\wallycode.exe provider gh-copilot-claude --model <model-from-previous-list>
```

Acceptance criteria:
- Exit code is 0 for each command.
- C:\src\ScratchTodo\wallycode.json contains the selected provider and model.

```powershell
$settings = Get-Content C:\src\ScratchTodo\wallycode.json -Raw | ConvertFrom-Json
($settings.provider -eq 'gh-copilot-claude')
($settings.model -eq '<model-from-previous-list>')
```

## Step 4: Create the initial project

Use `act` for a focused one-shot scaffold request:

```powershell
.\wallycode.exe act "Create a new .NET console solution named ScratchTodo. Put the app in src/ScratchTodo, add it to ScratchTodo.sln, and add a README with build and run commands." --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\ScratchTodo\ScratchTodo.sln exists.
- C:\src\ScratchTodo\src\ScratchTodo\Program.cs exists.
- C:\src\ScratchTodo\README.md exists.
- C:\src\ScratchTodo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\ScratchTodo\ScratchTodo.sln
Test-Path C:\src\ScratchTodo\src\ScratchTodo\Program.cs
Test-Path C:\src\ScratchTodo\README.md
Test-Path C:\src\ScratchTodo\.wallycode\session.json
```

## Step 5: Continue from the same active folder

Follow-up work can also be run from the exe folder without repeating `--source`:

```powershell
.\wallycode.exe act "Add command-line parsing for add/list/complete todo commands and update the README usage examples." --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- The command operates on C:\src\ScratchTodo because it is the active source.
- The previous session is archived automatically if it was terminal, and the new session is stored under C:\src\ScratchTodo\.wallycode.

```powershell
Test-Path C:\src\ScratchTodo\.wallycode\archive
Test-Path C:\src\ScratchTodo\.wallycode\session.json
```

## Step 6: Use a planned workflow for larger apps

Use `run` with the default requirements workflow when the app needs planning before implementation:

```powershell
.\wallycode.exe run "Create a minimal ASP.NET Core API from scratch with health, todo CRUD, README instructions, and a simple test project. Ask before choosing storage." --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- If WallyCode needs a decision, the session status is blocked and `respond` can continue it.
- If the workflow completes, project files are created under C:\src\ScratchTodo.

When blocked:

```powershell
.\wallycode.exe respond "Use in-memory storage for now and keep the API minimal." --log --verbose
```

If still active:

```powershell
.\wallycode.exe resume --log --verbose
```

## Cleanup

To remove WallyCode state from the active scratch folder:

```powershell
.\wallycode.exe cleanup
```

Acceptance criteria:
- C:\src\ScratchTodo\wallycode.json does not exist.
- C:\src\ScratchTodo\.wallycode does not exist.
- If C:\src\ScratchTodo was active, `wallycode.active.json` next to the exe is removed.
- Project files such as ScratchTodo.sln and Program.cs remain in place.

```powershell
Test-Path C:\src\ScratchTodo\wallycode.json
Test-Path C:\src\ScratchTodo\.wallycode
Test-Path C:\src\ScratchTodo\ScratchTodo.sln
```