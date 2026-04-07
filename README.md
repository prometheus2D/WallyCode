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

You can also reset from inside the shell later with:

```powershell
reset-memory
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
- `reset-memory` inside the shell clears the active workspace on demand
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

Reset the workspace from inside the shell:

```powershell
reset-memory
```

Reset a specific workspace from inside the shell:

```powershell
reset-memory --memory-root .wallycode-requirements
```

Start the shell with an isolated workspace location:

```powershell
wallycode shell --memory-root .wallycode-requirements --reset-memory
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

Use `--memory-root` on `shell` or `loop` to store session state in a different folder.