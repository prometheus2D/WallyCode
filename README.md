# WallyCode

Deterministic CLI workflows for getting real progress on a codebase with durable session state.

## Fastest path to value

Use this when you want results in minutes.

```powershell
# 1) Initialize a target repository (--source only needed here)
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

**Optional flags** (all commands above support these):
- `--log` — Write workflow logs to `.wallycode/logs/`
- `--verbose` — Include step-by-step output during execution
- `--memory-root <path>` — Use an alternate session directory (default: `.wallycode`)
- `--source <path>` — Override the default source path from wallycode.json
- `--provider <name>` — Override the default provider
- `--model <name>` — Override the default model
- `--max-run-iterations <n>` — Limit iterations for this run (default from wallycode.json or 3)

## Setup first: why it matters

The `setup` command is the one-time step that defines your project context.

**What setup does:**
- Creates wallycode.json to store provider, model, and iteration defaults.
- Creates .wallycode to store all session state and artifacts.
- Persists the source path in wallycode.json so future commands don't need `--source` (unless you want to override it).

**Why this matters:**
- **Simplicity**: After setup, `wallycode ask "..."` works from that directory; no need to repeat `--source C:\src\MyRepo --provider gh-copilot-claude --model <model>` every time.
- **Predictability**: All commands use the same provider and model unless explicitly overridden.
- **Consistency**: Session snapshots, memory, and logs are always in one place (.wallycode) so you can pause, resume, and recover workflows reliably.

**Starting fresh:**

If you want to reset to a clean workspace state, use:

```powershell
wallycode setup --source C:\src\MyRepo --force
```

This deletes and recreates wallycode.json and .wallycode, clearing all previous sessions and state. Useful for testing, troubleshooting, or starting a new workflow iteration.

Always run setup first on any new repository or when you want to reset your workflow context. See [tutorials/setup.md](tutorials/setup.md) for the step-by-step guide.

## Mental model

- setup creates project defaults and runtime state.
- provider locks in the default backend and model.
- run starts or continues a durable workflow session.
- respond unblocks and automatically resumes.
- resume continues the active session.

State lives in wallycode.json and .wallycode.

## Use tutorials for explicit, testable steps

The root README is intentionally minimal. Detailed command-by-command flows, expected outcomes, and testable checks are in the tutorial docs:

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

