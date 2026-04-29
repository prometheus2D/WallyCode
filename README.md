# WallyCode

Routed CLI agent that drives a repository through explicit LLM state transitions.

Built-in providers: `gh-copilot-claude`, `gh-copilot-gpt5`.

---

## Quick start

End-to-end against a repo at `C:\src\MyRepo`:

```powershell
wallycode setup --directory C:\src\MyRepo
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model claude-sonnet-4 --source C:\src\MyRepo
wallycode loop "Summarize this repository." --source C:\src\MyRepo --log --verbose
wallycode loop --source C:\src\MyRepo --log --verbose
```

If the session blocks for input:

```powershell
wallycode respond "Focus on routing and tests." --source C:\src\MyRepo
wallycode loop --source C:\src\MyRepo --log --verbose
```

Pick a workflow definition explicitly (`ask`, `act`, `requirements` (default)):

```powershell
wallycode loop "What does this project do?" --definition ask --source C:\src\MyRepo
wallycode loop "Refactor the routing code." --definition act --source C:\src\MyRepo
```

Run several transitions in one call, or isolate session state:

```powershell
wallycode loop "Review repo structure." --steps 3 --source C:\src\MyRepo --log --verbose
wallycode loop "Analyze docs." --source C:\src\MyRepo --memory-root C:\temp\wally-session-a
```

Inspect / refresh providers and models:

```powershell
wallycode provider --source C:\src\MyRepo
wallycode provider gh-copilot-gpt5 --models --source C:\src\MyRepo
wallycode provider gh-copilot-gpt5 --refresh --source C:\src\MyRepo
```

---

## Development mode

Use development mode when you are modifying WallyCode itself and want to run the current source build instead of an installed executable.

```powershell
dotnet restore WallyCode.sln
dotnet build WallyCode.sln
dotnet test WallyCode.sln
dotnet run --project WallyCode.Console -- help
```

Point the local build at this repository with `--source .`. Keep development sessions isolated with an ignored `.wallycode-dev` memory root:

```powershell
dotnet run --project WallyCode.Console -- setup --directory .
dotnet run --project WallyCode.Console -- ask "Explain the workflow command surface." --source . --memory-root .wallycode-dev --log --verbose
dotnet run --project WallyCode.Console -- act "Update the README tutorial links." --source . --memory-root .wallycode-dev --log --verbose
```

For repeated commands while editing:

```powershell
dotnet run --project WallyCode.Console -- shell --source . --memory-root .wallycode-dev --log --verbose
```

See [readmes/development-mode.md](readmes/development-mode.md) for the full development-mode workflow.

---

## Tutorial readmes

Task-focused guides live under [readmes/README.md](readmes/README.md):

- [Setup and providers](readmes/setup.md)
- [Ask workflow](readmes/ask.md)
- [Act workflow](readmes/act.md)
- [Definitions and steps](readmes/definitions.md)
- [Development mode](readmes/development-mode.md)
- [Stepwise workflows](readmes/stepwise.md)

---

## Mental model

Repo-scoped. `--source` selects which repo's config/state is used.

- `wallycode.json` -> repo configuration (default provider/model, etc.)
- `.wallycode\session.json` -> active session
- `.wallycode\archive\...` -> completed sessions
- `--memory-root <path>` -> override session state location for parallel sessions against the same repo

Session lifecycle inside `loop`:
- no active session -> `goal` is required, `--provider`/`--model` apply at start
- active session -> omit `goal` to continue
- blocked -> `respond`, then `loop`
- terminal -> archived automatically before a new one starts

Control keywords (shared):
- `[CONTINUE]` stay in current unit
- `[ASK_USER]` block until `respond`
- `[DONE]` complete workflow
- `[ERROR]` stop; reason goes in `summary`

Workflow-specific routing keywords (e.g. `[REQUIREMENTS_READY]`, `[TASKS_READY]`) move between units.

---

## Commands

### `setup`
```powershell
wallycode setup [--directory <path>] [--vs-build] [--force]
```
Initializes `wallycode.json` and `.wallycode`. `--vs-build` resolves the target from a build output path. `--force` regenerates artifacts.

### `provider`
```powershell
wallycode provider [name] [--set] [--models] [--refresh] [--model <model>] [--source <path>]
```
- no `name` -> list providers
- `name --set` -> set repo default provider
- `name --models` -> list models
- `name --refresh` -> refresh stored model catalog
- `name --model <model>` -> set repo default model
- `name` omitted for `--models|--refresh|--model` -> uses repo default provider

### `loop`
```powershell
wallycode loop [goal] [--definition <name>] [--provider <name>] [--model <model>]
               [--source <path>] [--memory-root <path>] [--steps <n>] [--log] [--verbose]
```

### `respond`
```powershell
wallycode respond <response> [--source <path>] [--memory-root <path>] [--log] [--verbose]
```

---

## Observability

`--log --verbose` on `loop` traces prompt text, raw provider output, selected keyword, next unit, session status, and `[ERROR]` reason. Use `--steps 1` while tuning prompts or routing.

---

## Failure modes

- Provider unavailable -> `wallycode provider --source <repo>`; verify external tooling is installed/authenticated
- No active session -> start with `wallycode loop "<goal>" --source <repo>`
- Blocked -> `respond` then `loop`
- `[ERROR]` -> inspect logged summary; adjust goal/definition/workspace and retry
- Wrong repo / session -> verify `--source` and `--memory-root`

