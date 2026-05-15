# Scratch Project From A New Folder

Use this tutorial to point WallyCode at a new folder and have it create a small program from scratch.

This proves the basic scratch workflow: setup creates or selects a folder, `wallycode.active.json` points WallyCode at that folder, and later commands keep operating there without repeating `--source`.

Run `.\wallycode.exe` from the folder that contains the exe.

## Prerequisites

Required:
- A terminal is open in the folder that contains `wallycode.exe`.
- Provider setup is complete, or you are ready to set provider/model in this tutorial.
- A browser is available to open the generated HTML file.

## Inputs

- Required: target folder path.
- Required: prompt describing the project to create.
- Optional: provider and model choice.

Example values used below:
- Scratch folder: C:\src\ScratchTicTacToe
- Provider: gh-copilot-claude
- Model: claude-haiku-4.5

## Step 1: Initialize the new folder

```powershell
.\wallycode.exe setup --source C:\src\ScratchTicTacToe
```

Acceptance criteria:
- Exit code is 0.
- C:\src\ScratchTicTacToe exists, even if it did not exist before setup.
- C:\src\ScratchTicTacToe\wallycode.json exists.
- C:\src\ScratchTicTacToe\.wallycode exists.
- `wallycode.active.json` next to the exe points to C:\src\ScratchTicTacToe.

```powershell
Test-Path C:\src\ScratchTicTacToe
Test-Path C:\src\ScratchTicTacToe\wallycode.json
Test-Path C:\src\ScratchTicTacToe\.wallycode
```

## Step 2: Confirm the active source

Run this from the exe folder:

```powershell
.\wallycode.exe status
```

Acceptance criteria:
- Exit code is 0.
- Output shows C:\src\ScratchTicTacToe as the active source.
- Output shows session state under C:\src\ScratchTicTacToe\.wallycode.

## Step 3: Set provider and model

These commands use the active source and do not need `--source`:

```powershell
.\wallycode.exe provider gh-copilot-claude --set
.\wallycode.exe provider gh-copilot-claude --models
.\wallycode.exe provider gh-copilot-claude --model claude-haiku-4.5
```

Acceptance criteria:
- Exit code is 0 for each command.
- C:\src\ScratchTicTacToe\wallycode.json contains the selected provider and model.

```powershell
$settings = Get-Content C:\src\ScratchTicTacToe\wallycode.json -Raw | ConvertFrom-Json
($settings.provider -eq 'gh-copilot-claude')
($settings.model -eq 'claude-haiku-4.5')
```

## Step 4: Create a simple program from scratch

Use `act` for a focused one-shot scaffold request. This example asks WallyCode to create a small browser-only Tic Tac Toe program in the active source folder.

```powershell
.\wallycode.exe act "Create a simple browser Tic Tac Toe game from scratch in this folder. Use index.html, styles.css, game.js, and README.md. The game should support two local players, show whose turn it is, detect wins and draws, and include a reset button." --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- C:\src\ScratchTicTacToe\index.html exists.
- C:\src\ScratchTicTacToe\styles.css exists.
- C:\src\ScratchTicTacToe\game.js exists.
- C:\src\ScratchTicTacToe\README.md exists.
- C:\src\ScratchTicTacToe\.wallycode\session.json exists.

```powershell
Test-Path C:\src\ScratchTicTacToe\index.html
Test-Path C:\src\ScratchTicTacToe\styles.css
Test-Path C:\src\ScratchTicTacToe\game.js
Test-Path C:\src\ScratchTicTacToe\README.md
Test-Path C:\src\ScratchTicTacToe\.wallycode\session.json
```

Manual check:
- Open C:\src\ScratchTicTacToe\index.html in a browser.
- Play a game and confirm turns, wins, draws, and reset behavior work.

## Step 5: Continue from the same defined folder

Follow-up work can also be run from the exe folder without repeating `--source`:

```powershell
.\wallycode.exe act "Improve the Tic Tac Toe game by adding score tracking for X wins, O wins, and draws. Update the README with how to open and play the game." --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- The command operates on C:\src\ScratchTicTacToe because it is the active source.
- The previous session is archived automatically if it was terminal, and the new session is stored under C:\src\ScratchTicTacToe\.wallycode.
- C:\src\ScratchTicTacToe\index.html, styles.css, game.js, and README.md remain the project files being changed.

```powershell
Test-Path C:\src\ScratchTicTacToe\.wallycode\archive
Test-Path C:\src\ScratchTicTacToe\.wallycode\session.json
```

## Step 6: Use a planned workflow for a larger scratch idea

Use `run` with the default requirements workflow when the scratch request needs planning before implementation:

```powershell
.\wallycode.exe run "Plan and then improve this Tic Tac Toe project with a simple computer opponent. Ask before choosing the opponent strategy." --log --verbose
```

Acceptance criteria:
- Exit code is 0.
- If WallyCode needs a decision, the session status is blocked and `respond` can continue it.
- If the workflow completes, project files are updated under C:\src\ScratchTicTacToe.

When blocked:

```powershell
.\wallycode.exe respond "Use a simple blocking strategy before choosing random moves." --log --verbose
```

If still active:

```powershell
.\wallycode.exe resume --log --verbose
```

Verification commands:

```powershell
.\wallycode.exe status
Test-Path C:\src\ScratchTicTacToe\.wallycode\session.json
```

## Cleanup

To remove WallyCode state from the active scratch folder:

```powershell
.\wallycode.exe cleanup
```

Acceptance criteria:
- C:\src\ScratchTicTacToe\wallycode.json does not exist.
- C:\src\ScratchTicTacToe\.wallycode does not exist.
- If C:\src\ScratchTicTacToe was active, `wallycode.active.json` next to the exe is removed.
- Project files such as index.html, styles.css, game.js, and README.md remain in place.

```powershell
Test-Path C:\src\ScratchTicTacToe\wallycode.json
Test-Path C:\src\ScratchTicTacToe\.wallycode
Test-Path C:\src\ScratchTicTacToe\index.html
```

Expected output is `False`, `False`, then `True`.

## Support notes

This flow is supported by the current runtime:
- setup creates the target folder if it does not already exist.
- setup writes `wallycode.active.json` next to the exe.
- commands without `--source` resolve the active source from `wallycode.active.json`.
- provider execution runs with the active source as the working directory and adds that folder as source context.

Known constraints:
- WallyCode is not assumed to be on `PATH`, so examples use `.\wallycode.exe` from the exe folder.
- The active source is one pointer per exe folder. Running setup for another folder changes where later no-`--source` commands operate.
- If the active folder is deleted or moved, run setup again with the new target path.