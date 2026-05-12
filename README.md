# WallyCode

Routed CLI agent that runs deterministic workflow iterations over a repository.

Built-in providers:
- gh-copilot-claude
- gh-copilot-gpt

## 5-minute quick start

Target repo in this example: C:\src\MyRepo

1. Initialize the repo for WallyCode.

```powershell
wallycode setup --directory C:\src\MyRepo
```

Expected result:
- Creates C:\src\MyRepo\wallycode.json.
- Creates C:\src\MyRepo\.wallycode for session/runtime state.

2. Pick a default provider and model.

```powershell
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
wallycode provider gh-copilot-claude --models --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model <model-from-previous-list> --source C:\src\MyRepo
```

Expected result:
- Provider and model defaults are saved in wallycode.json.

3. Start a workflow session.

```powershell
wallycode run "Summarize this repository." --source C:\src\MyRepo --log --verbose
```

Expected result:
- Starts a requirements workflow session.
- Runs up to 20 iterations in this invocation by default.
- Session state is persisted under .wallycode.

4. Continue if still active.

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Expected result:
- Continues the active session until it stops, blocks, errors, or reaches invocation limits.

5. If blocked, respond and auto-resume.

```powershell
wallycode respond "Focus on routing and docs." --source C:\src\MyRepo --log --verbose
```

Expected result:
- Saves your response and resumes the blocked session immediately.

## Command map

### setup

```powershell
wallycode setup [--directory <path>] [--vs-build] [--force]
```

Inputs:
- Optional directory target.
- Optional vs-build mode to resolve workspace from build output.
- Optional force recreation.

What happens:
- Initializes or refreshes wallycode.json and .wallycode.

### provider

```powershell
wallycode provider [name] [--set] [--models] [--refresh] [--model <model>] [--source <path>]
```

Inputs:
- Optional provider name.
- Action flags: set, models, refresh, model.
- Optional source path.

What happens:
- No action flag: lists providers with readiness.
- name plus --set: sets repo default provider.
- --models: lists models for selected or default provider.
- --refresh: refreshes model catalog for selected or default provider.
- --model <model>: sets repo default model for selected or default provider.

### status

```powershell
wallycode status [--source <path>] [--memory-root <path>]
```

Inputs:
- Optional source path.
- Optional memory-root override.

What happens:
- Prints the resolved source, memory root, default provider, and model.
- Prints active session state (workflow, step, iteration, goal) if a session exists.

### logging

```powershell
wallycode logging [--source <path>] [--enable|--disable] [--verbose|--quiet]
```

Inputs:
- Optional source path.
- Optional logging on/off flag.
- Optional verbose on/off flag.

What happens:
- Persists workspace logging defaults in repo settings.

### run

```powershell
wallycode run [prompt] [workflow] [--workflow <name>] [--provider <name>] [--model <model>]
              [--prompt <text>] [--action <text>] [--source <path>] [--memory-root <path>] [--max-run-iterations <n>]
              [--max-total-iterations <n>] [--max-step-repeats <n>] [--log] [--verbose]
```

Inputs:
- Optional prompt if starting a new session.
- Optional --prompt or --action text, equivalent to positional prompt.
- Optional workflow name; default is requirements.
- Optional provider/model overrides.
- Optional source and memory-root.
- Optional iteration guards.

What happens:
- Starts or continues one durable workflow session.
- Uses default max-run-iterations 20 per invocation.
- Stops early on stop, ask_user, error, or limits.

### ask

```powershell
wallycode ask [prompt] [--provider <name>] [--model <model>] [--source <path>]
              [--prompt <text>] [--action <text>] [--memory-root <path>] [--max-run-iterations <n>] [--max-total-iterations <n>]
              [--max-step-repeats <n>] [--log] [--verbose]
```

What happens:
- Shortcut for run with workflow ask.

### act

```powershell
wallycode act [prompt] [--provider <name>] [--model <model>] [--source <path>]
              [--prompt <text>] [--action <text>] [--memory-root <path>] [--max-run-iterations <n>] [--max-total-iterations <n>]
              [--max-step-repeats <n>] [--log] [--verbose]
```

What happens:
- Shortcut for run with workflow act.

### resume

```powershell
wallycode resume [--source <path>] [--memory-root <path>] [--max-run-iterations <n>]
                 [--max-total-iterations <n>] [--max-step-repeats <n>] [--log] [--verbose]
```

What happens:
- Continues the active session only.

### respond

```powershell
wallycode respond <response> [--source <path>] [--memory-root <path>] [--max-run-iterations <n>]
                  [--action <text>] [--prompt <text>] [--max-total-iterations <n>] [--max-step-repeats <n>] [--log] [--verbose]
```

What happens:
- Appends a response to a blocked session and resumes.

### recover

```powershell
wallycode recover [action] [--action <text>] [--prompt <text>] [--source <path>] [--memory-root <path>]
                 [--max-run-iterations <n>] [--max-total-iterations <n>] [--max-step-repeats <n>]
                 [--log] [--verbose]
```

What happens:
- Requires a terminal session (`error` or `completed`) in the selected memory root.
- Archives that terminal session.
- Starts a new run on the same workflow/provider/model using your recovery text.

### step

```powershell
wallycode step <prompt> [step] [--step <name>] [--provider <name>] [--model <model>]
               [--prompt <text>] [--action <text>] [--source <path>] [--memory-root <path>] [--log] [--verbose]
```

What happens:
- Runs one shared step directly without advancing a durable workflow session.

### shell

```powershell
wallycode shell [--source <path>] [--memory-root <path>] [--vs-build] [--reset-memory] [--log] [--verbose]
```

What happens:
- Starts an interactive shell with shared defaults across subcommands.

Shell built-ins (no executable prefix needed):
- `status` — print current source, memory root, provider, model, and session state.
- `reset-memory` — delete the active session and all snapshots.
- `exit` — quit the shell.

## Runtime files

- wallycode.json: repo-scoped defaults and provider catalog.
- .wallycode\session.json: current active session.
- .wallycode\sessions\session-000N.json: per-iteration snapshots.
- .wallycode\archive\...: archived terminal sessions.
- --memory-root <path>: alternate runtime root for isolated/parallel sessions.

## Tutorial guides

- [Readmes index](readmes/README.md)
- [Setup and providers](readmes/setup.md)
- [Ask workflow](readmes/ask.md)
- [Act workflow](readmes/act.md)
- [Definitions and steps](readmes/definitions.md)
- [Stepwise workflows](readmes/stepwise.md)
- [Development mode](readmes/development-mode.md)

