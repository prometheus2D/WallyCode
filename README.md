# WallyCode

WallyCode wraps `gh copilot` in two modes:

- `prompt`: one-shot response
- `loop`: stateful iterative execution with on-disk memory

## Requirements

- .NET 8 SDK
- GitHub CLI with `gh copilot`
- GitHub CLI authentication

## Core Model

- `prompt` returns one response
- `loop <goal>` starts a session
- `loop` continues the active session
- `--memory-root` creates an isolated workspace
- `--model` overrides the model for one run

There is no separate `resume` or `continue` command.

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

Start a loop:

```powershell
wallycode loop "Analyze this repo, do one bounded chunk of work, refresh memory, and stop when the goal is complete."
```

Continue the active loop:

```powershell
wallycode loop
```

Run multiple iterations in one invocation:

```powershell
wallycode loop --steps 3
```

Use a different source path:

```powershell
wallycode loop "Work on issue 123" --source C:\path\to\repo
```

Use a separate workspace:

```powershell
wallycode loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

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
  memory/
    goal.md
    current-tasks.md
    perspectives.md
    next-steps.md
    current-state.md
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

- `session.json`: active session metadata
- `memory/goal.md`: original goal
- `memory/current-tasks.md`: current working task list
- `memory/perspectives.md`: persistent guidance
- `memory/next-steps.md`: next actions
- `memory/current-state.md`: latest summarized state
- `logs/session.log`: session console log
- `logs/iteration-###.md`: normalized iteration summary and work log
- `prompts/iteration-###.txt`: exact prompt sent to the provider
- `raw/iteration-###.txt`: raw provider response

## Session Constraints

A loop session is bound to:

- goal
- source path
- provider
- model

If those differ, start a separate session with `--memory-root`.

Starting a new session resets:

- `memory/`
- `logs/`
- `prompts/`
- `raw/`

## Execution Flow

1. Resolve project root and provider
2. Open or create the workspace
3. Start a new session if a goal is provided, otherwise continue the active session
4. Read memory documents
5. Build one prompt
6. Send it to `gh copilot`
7. Save prompt, raw output, and iteration log
8. Parse JSON if possible
9. Normalize plain text if needed
10. Persist updated session state

## Provider Presets

- `gh-copilot-claude` -> `claude-sonnet-4`
- `gh-copilot-gpt5` -> `gpt-5`

Default provider if unset:

- `gh-copilot-claude`

## Provider Invocation

```text
gh copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>