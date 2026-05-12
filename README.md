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
wallycode run "Summarize this repository." --source C:\src\MyRepo --log --verbose
wallycode resume --source C:\src\MyRepo --log --verbose
```

If the session blocks for input:

```powershell
wallycode respond "Focus on routing and docs." --source C:\src\MyRepo --log --verbose
```

Pick a workflow definition explicitly (`ask`, `act`, `requirements` (default)):

```powershell
wallycode run "What does this project do?" ask --source C:\src\MyRepo
wallycode run "Refactor the routing code." act --source C:\src\MyRepo
wallycode run "Refactor the routing code." --workflow act --source C:\src\MyRepo
```

Run one shared step directly:

```powershell
wallycode step "Review the current workspace changes." review_changes --source C:\src\MyRepo
```

Run several workflow iterations in one call, or isolate session state:

```powershell
wallycode run "Review repo structure." requirements --max-run-iterations 3 --source C:\src\MyRepo --log --verbose
wallycode act "Fix these code problems: ..." --source C:\src\MyRepo --log --verbose
wallycode run "Analyze docs." --source C:\src\MyRepo --memory-root C:\temp\wally-session-a
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
- `.wallycode\sessions\session-000N.json` -> per-iteration session snapshots
- `.wallycode\archive\...` -> completed sessions
- `--memory-root <path>` -> override session state location for parallel sessions against the same repo

Session lifecycle inside `run`:
- no active session -> `prompt` is required, `--provider`/`--model` apply at start
- active session -> use `resume` or `run` without a prompt to continue
- blocked -> `respond` saves the answer and resumes automatically
- terminal -> archived automatically before a new one starts

The provider returns a `selectedStep` from the allowed step options in the prompt:
- `continue` stays on the current step
- a configured transition such as `produce_tasks` moves to its `targetStepName`
- `stop` completes the workflow
- `ask_user` blocks until `respond`
- `error` stops; reason goes in `summary`

For implementation work, `act` uses an implementation/review loop. The `act` step makes changes and writes `implementation`; `review_changes` reads that summary, reviews the workspace, and either chooses `stop`, `continue`, `ask_user`, or routes back to `act` with `review` feedback.

Workflow definitions live under `WallyCode.Console/Workflow/Definitions`. A definition declares workflow-level instructions, aliases, the start step, and the allowed step IDs. Those step IDs define the route options available in that workflow; transitions to outside targets are filtered out before prompting and resolution. Shared steps live under `WallyCode.Console/Workflow/Steps`, reusable transitions live under `WallyCode.Console/Workflow/Transitions`, and each step opts into routes with `transitionIds`.

Steps can also return a top-level `memory` object. The orchestrator merges that object into session memory, stores it in `session.json`, and injects declared memory keys into later step prompts.

Workflow execution is orchestrated in layers:

- `WorkflowOrchestrator` owns one deterministic session iteration.
- `WorkflowDefinition` owns the higher-level workflow instructions and allowed step graph.
- Step executors run the active step. Provider steps call the LLM; script steps run deterministic local scripts.
- `TransitionResolver` checks explicit guarded transitions first, then uses the LLM-selected transition and enforces derived handoff memory requirements.
- Session memory is filtered through each step's `writesMemory` contract before persistence.

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

### `run`
```powershell
wallycode run [prompt] [workflow] [--workflow <name>] [--provider <name>] [--model <model>]
              [--source <path>] [--memory-root <path>] [--max-run-iterations <n>]
              [--max-total-iterations <n>] [--max-step-repeats <n>]
              [--log] [--verbose]
```

By default, `run` allows up to 20 workflow iterations per invocation and stops early when the workflow completes, blocks for input, or errors. Use `--max-run-iterations` to lower or raise that invocation limit.

Use `--max-total-iterations` to cap total iterations across the active session lifetime. Use `--max-step-repeats` to cap how many times the same step can execute in one invocation. A value of `0` disables each cap.

The default workflow is `requirements`. The optional positional `workflow` and `--workflow` option select a workflow definition.

### `step`
```powershell
wallycode step <prompt> [step] [--step <name>] [--provider <name>] [--model <model>]
               [--source <path>] [--memory-root <path>] [--log] [--verbose]
```

Runs one shared step directly without advancing a durable workflow session. The default step is `ask`. If an active session exists at the selected memory root, declared `readsMemory` keys can be read for context, but step output does not mutate the workflow session.

### `respond`
```powershell
wallycode respond <response> [--source <path>] [--memory-root <path>] [--max-run-iterations <n>]
                  [--max-total-iterations <n>] [--max-step-repeats <n>]
                  [--log] [--verbose]
```

Saves a response for a blocked workflow session and immediately resumes it. Use `--max-run-iterations` to control how far it may continue after the response.

---

## Observability

`--log --verbose` on `run` traces prompt text, raw provider output, selected step, next step, session status, and `error` reason. Use `--max-run-iterations 1` while tuning prompts or routing. On `step`, verbose logging traces the single step prompt and raw provider output.

---

## Failure modes

- Provider unavailable -> `wallycode provider --source <repo>`; verify external tooling is installed/authenticated
- No active session -> start with `wallycode run "<prompt>" --source <repo>`
- Blocked -> `respond` saves the answer and resumes automatically
- `error` -> inspect logged summary; adjust goal/definition/workspace and retry
- Wrong repo / session -> verify `--source` and `--memory-root`

