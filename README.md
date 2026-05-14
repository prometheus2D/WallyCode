# WallyCode

Deterministic CLI workflows for getting real progress on a codebase with durable session state.

## Run from the exe folder against any source

WallyCode is not assumed to be on `PATH` yet. For now, open a terminal in the folder that contains `wallycode.exe` and run it as `.\wallycode.exe`.

The exe-local `wallycode.active.json` file remembers the active source directory. That means the command can be launched from the exe folder while WallyCode operates on a different source folder, and normal commands do not need `--source` once setup is complete.

### 1. Build or locate the exe

If you already have a built `wallycode.exe`, open a terminal in that folder. From source, publish the console app to a stable tools folder, then move into that folder:

```powershell
dotnet publish .\WallyCode.Console\WallyCode.Console.csproj -c Release -o C:\Tools\WallyCode
Set-Location C:\Tools\WallyCode
.\wallycode.exe help
```

All command examples below assume the terminal is still in the folder that contains `wallycode.exe`.

### 2. Initialize the source repo once

Run setup against the repository WallyCode should operate on:

```powershell
.\wallycode.exe setup --source C:\src\MyRepo
```

This creates `C:\src\MyRepo` if needed, adds `wallycode.json` and `.wallycode` inside it, then writes `wallycode.active.json` next to the exe. After that, WallyCode resolves the active source from that file when `--source` is omitted.

For a clean reset when WallyCode state already exists, add `--cleanup`:

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --cleanup
```

This removes existing `wallycode.json` and `.wallycode` for that source, recreates them with defaults, and updates `wallycode.active.json` to point at `C:\src\MyRepo`. It does not delete normal project files.

### 3. Configure provider and model

These commands now use the active source, so they can be run from the exe folder without changing into `C:\src\MyRepo`:

```powershell
.\wallycode.exe provider gh-copilot-claude --set
.\wallycode.exe provider gh-copilot-claude --models
.\wallycode.exe provider gh-copilot-claude --model claude-haiku-4.5
```

### 4. Use ask, act, or the workflow loop

Use `ask` for one-shot analysis:

```powershell
.\wallycode.exe ask "What does this project do?"
```

Use `act` for one-shot implementation:

```powershell
.\wallycode.exe act "Add a short README section describing how to run the project."
```

Use `run` for the multi-step workflow loop. The default `requirements` workflow moves through the three main work phases: requirements collection, task creation, and task execution.

```powershell
.\wallycode.exe status
.\wallycode.exe run "Build a simple browser Tic Tac Toe game with a README." requirements --log --verbose
```

If the session blocks, respond from the exe folder:

```powershell
.\wallycode.exe respond "Proceed with docs and routing first."
```

If the session is still active, resume it:

```powershell
.\wallycode.exe resume
```

If requirements are already clear and you want to start at task creation, use the `tasks` workflow:

```powershell
.\wallycode.exe run "Create tasks for adding score tracking, then implement them." tasks --log --verbose
```

## Setup model (current behavior)

Setup is required before normal command use in a workspace.

What setup creates:
- wallycode.json for provider/model/logging/runtime defaults.
- .wallycode for session and runtime state.
- wallycode.active.json next to the exe, pointing to the active source directory.

Commands that expect initialized setup:
- run, ask, act, step
- provider, logging, status, shell
- recover

Commands that require an existing session:
- resume
- respond

Notes:
- There is no global auto-setup on run/ask/act.
- If setup artifacts are missing, commands fail with an instruction to run setup.
- When --source is omitted, WallyCode uses wallycode.active.json to find the active initialized source directory.
- setup --vs-build resolves the source workspace root from a bin/Debug or bin/Release launch path.

## Common flags

Command-specific options vary, but these are common on workflow commands:
- --source <path> to override the active source for a command
- --memory-root <path>
- --log
- --verbose
- --max-run-iterations <n>
- --max-total-iterations <n>
- --max-step-repeats <n>

## Cleanup

To reset workspace state:

```powershell
.\wallycode.exe cleanup
```

This removes wallycode.json and .wallycode from the active source. If that source was active, it also clears wallycode.active.json.

## Mental model

- setup initializes workspace defaults, runtime state, and the exe-local active source pointer.
- cleanup removes workspace defaults and runtime state. If the cleaned source is active, it also clears the active source pointer.
- provider and logging update persisted workspace defaults.
- ask answers one question and stops.
- act performs one focused action and stops.
- run starts or continues the workflow loop. The default requirements workflow runs requirements collection, task creation, and task execution.
- respond/resume/recover operate on existing session state.

Project state lives in wallycode.json and .wallycode. The active project pointer lives in wallycode.active.json next to the exe.

## Use tutorials for explicit, testable steps

- [Tutorials index](tutorials/README.md)
- [Setup and providers](tutorials/setup.md)
- [Scratch project from a new folder](tutorials/scratch-project.md)
- [Ask workflow](tutorials/ask.md)
- [Act workflow](tutorials/act.md)
- [Stepwise workflows](tutorials/stepwise.md)
- [Definitions and steps](tutorials/definitions.md)
- [Development mode](tutorials/development-mode.md)

## Core commands

```powershell
.\wallycode.exe setup
.\wallycode.exe cleanup
.\wallycode.exe provider
.\wallycode.exe status
.\wallycode.exe run
.\wallycode.exe ask
.\wallycode.exe act
.\wallycode.exe resume
.\wallycode.exe respond
.\wallycode.exe recover
.\wallycode.exe step
.\wallycode.exe shell
```
