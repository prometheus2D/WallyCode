# WallyCode

WallyCode wraps `gh copilot` in two modes:

- `prompt`: one-shot response
- `loop`: stateful iterative execution with on-disk memory

## Core Model

- `prompt` returns one response
- `loop <goal>` starts a session
- `loop` continues the active session
- `loop --template <id>` selects loop behavior from JSON
- `respond <text>` appends user input for the active loop
- `--memory-root` creates an isolated workspace
- `--model` overrides the model for one run

There is no separate `resume` or `continue` command.

## Hello World: Verify the Requirements Loop

Use this sequence to verify the full loop is working.

1. Verify providers:

```powershell
wallycode providers
```

2. Start a requirements loop:

```powershell
wallycode loop "I want to make a tic tac toe game." --template requirements
```

Expected result:

- `.wallycode/` is created
- `session.json`, `state.json`, and `responses.json` are created
- memory files are created under `.wallycode/memory/`
- the first iteration writes follow-up questions into `memory/next-steps.md`

3. Answer the loop:

```powershell
wallycode respond "Make it a simple browser game for two human players. Keep it minimal. I approve once the requirements are clear."
```

Expected result:

- the response is appended to `memory/user-responses.md`
- a structured response entry is appended to `responses.json`

4. Continue the loop:

```powershell
wallycode loop
```

Expected result:

- only the new response is processed
- `state.json` is updated
- `memory/current-state.md` is updated
- `memory/next-steps.md` is updated

5. Finish the requirements loop:

```powershell
wallycode respond "approve"
wallycode loop
```

Expected result:

- the requirements template stop keyword matches
- the session becomes done
- `session.json` shows `isDone: true`

## Commands

List providers:

```powershell
wallycode providers
```

Set the repo default provider:

```powershell
wallycode set-provider gh-copilot-claude
```

Run one prompt:

```powershell
wallycode prompt "Summarize this repository in one short paragraph."
```

Start the default loop:

```powershell
wallycode loop "Analyze this repo, do one bounded chunk of work, refresh memory, and stop when the goal is complete."
```

Start the requirements loop:

```powershell
wallycode loop "Collect requirements for the new workspace flow." --template requirements
```

Append user answers for the active loop:

```powershell
wallycode respond "Use GitHub auth only. Support multiple isolated workspaces. Stop when I reply approve."
```

Continue the active loop:

```powershell
wallycode loop
```

Run multiple iterations in one invocation:

```powershell
wallycode loop --steps 3
```

## Loop Templates

Loop behavior is data-driven.

Built-in templates:

- `default`
- `requirements`

Template files are shipped as JSON and copied to the build output:

```plaintext
Templates/Loops/default.json
Templates/Loops/requirements.json
```

A template defines:

- system prompt
- response contract prompt
- initial memory documents
- optional stop keyword

## Workspace

Repo settings:

```plaintext
wallycode.json
```

Default loop workspace:

```plaintext
<repo>/.wallycode/
```

Layout:

```plaintext
.wallycode/
  session.json
  state.json
  responses.json
  memory/
    goal.md
    current-tasks.md
    perspectives.md
    next-steps.md
    current-state.md
    user-responses.md
  logs/
    session.log
    iteration-001.md
    iteration-002.md
    ...
  prompts/
    iteration-001.txt
    iteration-002.txt
    ...
  raw/
    iteration-001.txt
    iteration-002.txt
    ...
```

## File Roles

- `session.json`: active session metadata, including goal, provider, model, source path, next iteration, done state, and template id
- `state.json`: compact structured loop state for efficient resume behavior
- `responses.json`: structured user responses with ids and timestamps
- `memory/goal.md`: original goal
- `memory/current-tasks.md`: programmatically rendered current work view
- `memory/perspectives.md`: static template guidance
- `memory/next-steps.md`: programmatically rendered next actions
- `memory/current-state.md`: programmatically rendered state summary
- `memory/user-responses.md`: human-readable audit log of user answers
- `logs/session.log`: session console log
- `logs/iteration-###.md`: normalized iteration summary and structured output log
- `prompts/iteration-###.txt`: exact prompt sent to the provider
- `raw/iteration-###.txt`: raw provider response

## Goal Persistence

The goal is persisted in two places when a session starts:

- `.wallycode/session.json` as `goal`
- `.wallycode/memory/goal.md` as the goal document

That is why `loop <goal>` is only needed when creating a new session.
After that, `loop` reloads the active session from `session.json`, reconstructs `AppOptions` from the saved session state, and reuses the persisted goal automatically.

## Structured Loop State

`state.json` is intentionally small.
It exists to help loops resume efficiently without forcing the system to reconstruct all machine state from markdown.

Current fields:

- `phase`
- `openQuestions`
- `decisions`
- `stopKeywordMatched`
- `lastProcessedUserResponseAt`
- `lastProcessedUserResponseId`

This file is for compact machine state.
The markdown files remain the human-readable and model-readable memory layer.

## Structured User Responses

User responses are stored in two forms:

- `responses.json`: canonical machine-readable store
- `memory/user-responses.md`: human-readable audit log

The loop processes only responses with ids greater than `lastProcessedUserResponseId`.
This avoids rescanning the full response history every iteration.

## Compact Model Contract

The model no longer rewrites full memory documents every iteration.
It returns compact structured data:

- `status`
- `summary`
- `workLog`
- `questions`
- `decisions`
- `assumptions`
- `blockers`
- `doneReason`

WallyCode then renders the memory documents programmatically.
This reduces token usage, reduces drift, and keeps loop behavior more deterministic.

## Session Constraints

A loop session is bound to:

- goal
- source path
- provider
- model
- template

If those differ, start a separate session with `--memory-root`.

Starting a new session resets:

- `memory/`
- `logs/`
- `prompts/`
- `raw/`
- `state.json`
- `responses.json`

## Execution Flow

1. Resolve project root and provider
2. Open or create the workspace
3. Start a new session if a goal is provided, otherwise continue the active session
4. Load the loop template
5. Load compact loop state
6. Read memory documents and pending structured user responses
7. Build one prompt from template + memory state + pending responses
8. Send it to `gh copilot`
9. Save prompt and raw output
10. Parse compact structured JSON if possible
11. Update `state.json`
12. Render memory documents programmatically
13. Persist updated session state

## Stop Conditions

A loop can stop in three ways:

- the model returns `status = done`
- the model returns a blocking `doneReason`
- the active template defines a stop keyword and that keyword appears in pending user responses

Example: the `requirements` template uses `approve` as its stop keyword.

## Provider Presets

- `gh-copilot-claude` -> `claude-sonnet-4`
- `gh-copilot-gpt5` -> `gpt-5`

Default provider if unset:

- `gh-copilot-claude`

## Provider Invocation

```text
gh copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>