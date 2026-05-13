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

## Setup and cleanup model

Setup is recommended for predictable defaults, but not required for execution.

What setup does:
- Creates wallycode.json to store provider, model, and iteration defaults.
- Creates .wallycode to store all session state and artifacts.
- Persists the source path in wallycode.json so future commands don't need `--source` (unless you want to override it).

Why this matters:
- Simplicity: After setup, wallycode ask "..." works from that directory with stable defaults.
- Predictability: Provider and model remain fixed unless overridden.
- Consistency: Session snapshots, memory, and logs stay under .wallycode.

If setup was not run:
- Commands still run.
- Runtime session state is created lazily under .wallycode.
- wallycode.json is created only when a command persists settings.

Starting fresh:

If you want a clean workspace state, use:

```powershell
wallycode cleanup --source C:\src\MyRepo
```

This removes wallycode.json and .wallycode. You can recreate defaults with setup immediately after.

Use setup on new repositories when you want pinned defaults. See [tutorials/setup.md](tutorials/setup.md) for the full flow.

## Mental model

- setup creates project defaults and runtime state.
- cleanup removes project defaults and runtime state.
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

