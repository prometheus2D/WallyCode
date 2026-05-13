# WallyCode

Deterministic CLI workflows for getting real progress on a codebase with durable session state.

## Run from anywhere

WallyCode is designed to be launched from any directory after one setup command. The exe-local `wallycode.active.json` file remembers the active source directory, so normal commands do not need `--source` once setup is complete.

### 1. Make the command available

If you already have a built `wallycode.exe`, put that folder on your `PATH`.

From source, publish the console app to a stable tools folder, then add that folder to `PATH`:

```powershell
dotnet publish .\WallyCode.Console\WallyCode.Console.csproj -c Release -o C:\Tools\WallyCode
$env:Path = "C:\Tools\WallyCode;$env:Path"
wallycode help
```

The `PATH` command above updates the current terminal. Add `C:\Tools\WallyCode` to your user or system `PATH` for new terminals.

### 2. Initialize the source repo once

Run setup against the repository WallyCode should operate on:

```powershell
wallycode setup --source C:\src\MyRepo
```

This creates `wallycode.json` and `.wallycode` in `C:\src\MyRepo`, then writes `wallycode.active.json` next to the exe. After that, WallyCode resolves the active source from that file when `--source` is omitted.

### 3. Configure provider and model

These commands now use the active source, so they can be run from any directory:

```powershell
wallycode provider gh-copilot-claude --set
wallycode provider gh-copilot-claude --models
wallycode provider gh-copilot-claude --model claude-sonnet-4
```

### 4. Let WallyCode work

```powershell
wallycode status
wallycode run "Summarize architecture and propose next actions."
```

If the session blocks, respond from wherever you are:

```powershell
wallycode respond "Proceed with docs and routing first."
```

If the session is still active, resume it:

```powershell
wallycode resume
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
wallycode cleanup
```

This removes wallycode.json and .wallycode from the active source. If that source was active, it also clears wallycode.active.json.

## Mental model

- setup initializes workspace defaults, runtime state, and the exe-local active source pointer.
- cleanup removes workspace defaults and runtime state. If the cleaned source is active, it also clears the active source pointer.
- provider and logging update persisted workspace defaults.
- run/ask/act/step execute workflow logic against initialized workspace state.
- respond/resume/recover operate on existing session state.

Project state lives in wallycode.json and .wallycode. The active project pointer lives in wallycode.active.json next to the exe.

## Use tutorials for explicit, testable steps

- [Tutorials index](tutorials/README.md)
- [Setup and providers](tutorials/setup.md)
- [Ask workflow](tutorials/ask.md)
- [Act workflow](tutorials/act.md)
- [Stepwise workflows](tutorials/stepwise.md)
- [Definitions and steps](tutorials/definitions.md)
- [Development mode](tutorials/development-mode.md)

## Core commands

```powershell
wallycode setup
wallycode cleanup
wallycode provider
wallycode status
wallycode run
wallycode ask
wallycode act
wallycode resume
wallycode respond
wallycode recover
wallycode step
wallycode shell
```
