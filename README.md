# WallyCode

WallyCode is a routed CLI agent for working a repository through explicit LLM state transitions.

Core model:
- `setup` initializes a repo for WallyCode
- `provider` selects the default provider/model for that repo
- `loop` starts or advances a routed session
- `respond` unblocks a session when the agent asks for user input
- `--log --verbose` exposes prompts, responses, and transitions

Built-in providers in this workspace:
- `gh-copilot-claude`
- `gh-copilot-gpt5`

---

## Quick start

Initialize a repo:

```powershell
wallycode setup --directory C:\src\MyRepo
```

Inspect providers:

```powershell
wallycode provider --source C:\src\MyRepo
```

Set the repo default provider:

```powershell
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
```

Inspect models for that provider:

```powershell
wallycode provider gh-copilot-claude --models --source C:\src\MyRepo
```

Set the repo default model:

```powershell
wallycode provider gh-copilot-claude --model claude-sonnet-4 --source C:\src\MyRepo
```

Start a session:

```powershell
wallycode loop "Summarize this repository in one short paragraph." --source C:\src\MyRepo --log --verbose
```

Continue it:

```powershell
wallycode loop --source C:\src\MyRepo --log --verbose
```

If the session blocks for user input:

```powershell
wallycode respond "Focus on routing and tests." --source C:\src\MyRepo --log
wallycode loop --source C:\src\MyRepo --log --verbose
```

---

## Mental model

WallyCode is repo-scoped.

Repo configuration lives in:
- `wallycode.json`

Runtime/session state lives in:
- `.wallycode\session.json`
- `.wallycode\archive\...`
- transcript/log files when logging is enabled

`--source` selects the repo whose settings should be used.

`--memory-root` overrides where session state is stored. Use it when you want multiple independent sessions against the same repo.

---

## Commands

### `setup`

Initialize WallyCode artifacts for a target directory.

```powershell
wallycode setup [--directory <path>] [--vs-build] [--force]
```

Use cases:
- first-time repo initialization
- regenerating `wallycode.json` and `.wallycode`
- resolving the target from a VS build output path with `--vs-build`

Examples:

```powershell
wallycode setup --directory C:\src\MyRepo
wallycode setup --directory C:\src\MyRepo --force
```

### `provider`

Inspect providers, inspect models, refresh model catalogs, set repo defaults.

```powershell
wallycode provider [name] [--set] [--models] [--refresh] [--model <model>] [--source <path>]
```

Patterns:

List providers for a repo:

```powershell
wallycode provider --source C:\src\MyRepo
```

Set repo default provider:

```powershell
wallycode provider gh-copilot-gpt5 --set --source C:\src\MyRepo
```

List models for a provider:

```powershell
wallycode provider gh-copilot-gpt5 --models --source C:\src\MyRepo
```

Refresh the stored model catalog:

```powershell
wallycode provider gh-copilot-gpt5 --refresh --source C:\src\MyRepo
```

Set repo default model:

```powershell
wallycode provider gh-copilot-gpt5 --model gpt-5 --source C:\src\MyRepo
```

Notes:
- `--source` determines which repo’s `wallycode.json` is read/written
- if `name` is omitted for `--models`, `--refresh`, or `--model`, the current repo default provider is used
- provider/model defaults are repo settings, not global process settings

### `loop`

Run the routing engine.

```powershell
wallycode loop [goal] [--definition <name>] [--provider <name>] [--model <model>] [--source <path>] [--memory-root <path>] [--steps <n>] [--log] [--verbose]
```

Semantics:
- no active session: `goal` is required
- active session: omit `goal` to continue
- blocked session: use `respond`, then `loop`
- terminal session: WallyCode archives it before starting a new one
- `--provider` and `--model` matter when starting a new session
- `--steps` runs multiple transitions in one invocation

Examples:

Start with repo defaults:

```powershell
wallycode loop "Summarize this repository." --source C:\src\MyRepo
```

Start with a specific definition:

```powershell
wallycode loop "What does this project do?" --definition ask --source C:\src\MyRepo
wallycode loop "Refactor the routing code for readability." --definition act --source C:\src\MyRepo
```

Start with explicit provider/model overrides:

```powershell
wallycode loop "Summarize the tests." --provider gh-copilot-gpt5 --model gpt-5 --source C:\src\MyRepo
```

Run several transitions in one call:

```powershell
wallycode loop "Review repository structure." --steps 3 --source C:\src\MyRepo --log --verbose
```

Continue current session:

```powershell
wallycode loop --source C:\src\MyRepo
```

Use isolated session state:

```powershell
wallycode loop "Analyze docs." --source C:\src\MyRepo --memory-root C:\temp\wally-session-a
```

### `respond`

Append user input to the active session.

```powershell
wallycode respond <response> [--source <path>] [--memory-root <path>] [--log] [--verbose]
```

Example:

```powershell
wallycode respond "Prefer bullet points and keep it short." --source C:\src\MyRepo
```

Then resume:

```powershell
wallycode loop --source C:\src\MyRepo
```

---

## Definitions

Definitions are named routed workflows.

Examples currently exercised by tests:
- `ask`
- `act`

If omitted, `loop` defaults to:
- `requirements`

---

## Keywords

Shared control keywords are intentionally small:

- `[CONTINUE]` keeps work in the current logical unit
- `[ASK_USER]` blocks the session until `respond` provides input
- `[DONE]` completes the workflow
- `[ERROR]` stops the workflow because an unrecoverable problem occurred

Workflow-specific routing keywords such as `[REQUIREMENTS_READY]` and `[TASKS_READY]` move execution between logical units.

When `[ERROR]` is selected, the provider should put the user-visible reason in the `summary` field.

---

## Observability

If you want to see each prompt/response/transition, use logging.

```powershell
wallycode loop "Summarize this repository." --source C:\src\MyRepo --log --verbose
```

This is the current operator-facing trace surface for:
- prompt text sent to the provider
- raw provider output
- selected keyword per iteration
- next unit / session status
- error reason when a run ends with `[ERROR]`

Recommended pattern while tuning prompts or routing:

```powershell
wallycode loop "<goal>" --source <repo> --steps 1 --log --verbose
```

Single-step runs make transitions easier to inspect than large `--steps` batches.

---

## First-use flow

```powershell
wallycode setup --directory C:\src\MyRepo
wallycode provider --source C:\src\MyRepo
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model claude-sonnet-4 --source C:\src\MyRepo
wallycode loop "Summarize this repository in one short paragraph." --source C:\src\MyRepo --log --verbose
```

Blocked session flow:

```powershell
wallycode respond "Focus on routing and test structure." --source C:\src\MyRepo --log
wallycode loop --source C:\src\MyRepo --log --verbose
```

---

## Failure modes

Provider unavailable:
- inspect with `wallycode provider --source <repo>`
- verify the external provider tooling is installed/authenticated

No active session:
- start one with `wallycode loop "<goal>" --source <repo>`

Blocked session:
- `wallycode respond "<message>" --source <repo>`
- then `wallycode loop --source <repo>`

Workflow ended with `[ERROR]`:
- inspect the logged summary/error reason
- adjust the goal, definition, or workspace state before retrying

Wrong repo or wrong session state:
- verify `--source`
- verify `--memory-root`

