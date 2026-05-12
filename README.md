# WallyCode

Deterministic CLI workflows for getting real progress on a codebase with durable session state.

## Fastest path to value

Use this when you want results in minutes.

```powershell
# 1) Initialize a target repository
wallycode setup --directory C:\src\MyRepo

# 2) Set provider + model once
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
wallycode provider gh-copilot-claude --models --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model <model> --source C:\src\MyRepo

# 3) Start work
wallycode run "Summarize architecture and propose next actions." --source C:\src\MyRepo --log --verbose
```

If the session blocks:

```powershell
wallycode respond "Proceed with docs and routing first." --source C:\src\MyRepo --log --verbose
```

If still active:

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

## Mental model

- setup creates project defaults and runtime state.
- provider locks in the default backend and model.
- run starts or continues a durable workflow session.
- respond unblocks and automatically resumes.
- resume continues the active session.

State lives in wallycode.json and .wallycode.

## Use tutorials for explicit, testable steps

The root README is intentionally minimal. Detailed command-by-command flows, expected outcomes, and testable checks are in the tutorial docs:

- [Readmes index](readmes/README.md)
- [Setup and providers](readmes/setup.md)
- [Ask workflow](readmes/ask.md)
- [Act workflow](readmes/act.md)
- [Stepwise workflows](readmes/stepwise.md)
- [Definitions and steps](readmes/definitions.md)
- [Development mode](readmes/development-mode.md)

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

