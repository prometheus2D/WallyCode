# WallyCode

Deterministic CLI workflows for getting real progress on a codebase with durable session state.

## Fastest path to value

```powershell
# 1) Initialize a target repository
wallycode setup --source C:\src\MyRepo

# 2) Navigate to that directory
cd C:\src\MyRepo

# 3) Set provider + model once
wallycode provider gh-copilot-claude --set
wallycode provider gh-copilot-claude --models
wallycode provider gh-copilot-claude --model claude-sonnet-4

# 4) Start work
wallycode run "Summarize architecture and propose next actions."
```

If the session blocks:

```powershell
wallycode respond "Proceed with docs and routing first."
```

If still active:

```powershell
wallycode resume
```

## Setup model (current behavior)

Setup is required before normal command use in a workspace.

What setup creates:
- wallycode.json for provider/model/logging/runtime defaults.
- .wallycode for session and runtime state.

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
- setup --vs-build resolves the source workspace root from a bin/Debug or bin/Release launch path.

## Common flags

Command-specific options vary, but these are common on workflow commands:
- --source <path>
- --memory-root <path>
- --log
- --verbose
- --max-run-iterations <n>
- --max-total-iterations <n>
- --max-step-repeats <n>

## Cleanup

To reset workspace state:

```powershell
wallycode cleanup --source C:\src\MyRepo
```

This removes wallycode.json and .wallycode.

## Mental model

- setup initializes workspace defaults and runtime state.
- cleanup removes workspace defaults and runtime state.
- provider and logging update persisted workspace defaults.
- run/ask/act/step execute workflow logic against initialized workspace state.
- respond/resume/recover operate on existing session state.

State lives in wallycode.json and .wallycode.

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
