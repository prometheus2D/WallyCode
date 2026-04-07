# WallyCode

WallyCode wraps `gh copilot` in two modes:

- `prompt`: one-shot response
- `loop`: stateful iterative execution with on-disk memory

## Quick Start

1. Verify providers:

```powershell
wallycode providers
```

2. Start a clean shell session:

```powershell
wallycode shell --reset-memory
```

3. Inside the shell, start a requirements loop:

```powershell
loop "I want to make a tic tac toe game." --template requirements
```

4. Answer the loop:

```powershell
respond "Make it a simple browser game for two human players. Keep it minimal. I approve once the requirements are clear."
```

5. Continue until done:

```powershell
loop
respond "approve"
loop
```

Expected result:

- `.wallycode/` is recreated on shell start when `--reset-memory` is used
- loop state is stored under `.wallycode/`
- `respond` appends structured user input
- `loop` resumes the active session from disk

## Core Model

- `prompt` returns one response
- `loop <goal>` starts a session
- `loop` continues the active session
- `loop --template <id>` selects loop behavior from JSON
- `respond <text>` appends user input for the active loop
- `shell --reset-memory` clears `.wallycode` before interactive use
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

Start the shell:

```powershell
wallycode shell
```

Start the shell with a clean workspace:

```powershell
wallycode shell --reset-memory
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

## Loop Templates

Built-in templates:

- `default`
- `requirements`

Template files:

```plaintext
Templates/Loops/default.json
Templates/Loops/requirements.json
```

A template defines:

- system prompt
- response contract prompt
- initial memory documents
- optional stop keyword

## Provider Presets

- `gh-copilot-claude` -> `claude-sonnet-4`
- `gh-copilot-gpt5` -> `gpt-5`

Default provider if unset:

- `gh-copilot-claude`

## Provider Invocation

```text
gh copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>