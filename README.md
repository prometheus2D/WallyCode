# WallyCode

WallyCode is a small .NET 8 console app that wraps `gh copilot`.

Think about it in three parts:

- Setup: choose a provider and, if needed, override the model.
- One-off use: `prompt` is the current command for a single ask-style response.
- Looping use: `loop` starts a stateful session or continues the active one.

## Requirements

- .NET 8 SDK
- GitHub CLI installed
- GitHub CLI authenticated and able to run `gh copilot`

Before a prompt or loop run starts, WallyCode asks the selected provider to check whether it is ready. For the GitHub CLI provider, that means checking `gh`, `gh copilot`, and GitHub CLI authentication.

All commands below assume your current directory is the repo root. If you run them from somewhere else, adjust the `--project` path and, for repo-aware commands, add `--source <path-to-repo>`.

## Quick Start

Build the solution:

```powershell
dotnet build WallyCode.sln
```

Show top-level help:

```powershell
dotnet run --project .\WallyCode.Console -- help
```

Run one simple prompt:

```powershell
dotnet run --project .\WallyCode.Console -- prompt "Summarize this repository in one short paragraph."
```

If you are thinking in terms of an "ask" command, `prompt` is that one-off command today.

## Most Common Commands

These commands map to the three main things users do: setup, one-off usage, and looping.

Show help for one command:

```powershell
dotnet run --project .\WallyCode.Console -- help <command>
```

List the available providers:

```powershell
dotnet run --project .\WallyCode.Console -- providers
```

The `providers` command also shows whether each provider is ready to run on your machine.

Set the saved default provider for this repo:

```powershell
dotnet run --project .\WallyCode.Console -- set-provider gh-copilot-gpt5
```

Override the model for one prompt without changing the saved provider:

```powershell
dotnet run --project .\WallyCode.Console -- prompt "Summarize this repository in one short paragraph." --model gpt-5
```

Start a loop and do exactly one step:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Analyze this repo, do one bounded chunk of work, update memory, and stop when the goal is complete."
```

Continue that same loop and do one more step:

```powershell
dotnet run --project .\WallyCode.Console -- loop
```

`resume` and `continue` are aliases for `loop` if you prefer those words:

```powershell
dotnet run --project .\WallyCode.Console -- resume
dotnet run --project .\WallyCode.Console -- continue
```

Run more than one loop step in a single invocation:

```powershell
dotnet run --project .\WallyCode.Console -- loop --steps 3
```

Start a separate loop session in a different memory folder:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

## Step-By-Step Loop

Use the loop when you want progress to be saved to disk between steps.

1. Start a session with `loop <goal>`.
2. Inspect `.wallycode/` if you want to review what happened.
3. Continue with `loop`.
4. Keep resuming until the loop reports `done`.

The simplest mental model is:

- `prompt` is the simple one-off path.
- `loop <goal>` starts a session.
- `loop` continues that session.
- `resume` and `continue` are compatibility aliases.
- `loop` runs one iteration by default.
- Use `--steps <n>` only when you want more than one iteration in the current invocation.

## What WallyCode Writes

WallyCode stores project settings in `wallycode.json` at the repo root.

Loop runs write state under `.wallycode/`:

- `.wallycode/session.json` tracks the active loop session and the next iteration number.
- `.wallycode/memory/goal.md` stores the original goal.
- `.wallycode/memory/current-tasks.md` stores the current task list.
- `.wallycode/memory/perspectives.md` stores the perspective document.
- `.wallycode/memory/next-steps.md` stores the next-step list.
- `.wallycode/memory/current-state.md` stores the latest state summary.
- `.wallycode/prompts/iteration-###.txt` stores each prompt sent to the provider.
- `.wallycode/raw/iteration-###.txt` stores each raw provider response.
- `.wallycode/logs/iteration-###.md` stores the iteration summary and work log.
- `.wallycode/logs/session.log` stores the console log for the session.

Prompt runs also write files under `.wallycode/`:

- `.wallycode/prompts/prompt-*.txt` stores the one-off prompt text.
- `.wallycode/raw/prompt-*.txt` stores the raw one-off provider response.
- `.wallycode/logs/prompt-*.log` stores the console log for the one-off run.

## Providers and Models

Current provider presets:

- `gh-copilot-claude` uses `claude-sonnet-4`
- `gh-copilot-gpt5` uses `gpt-5`

If you never set a provider, WallyCode defaults to `gh-copilot-claude`.

Use `providers` to list them.

Use `set-provider <name>` to change the saved default provider for the repo.

Use `--model <name>` on `prompt` or `loop` when you want a one-off model override without changing the saved provider.

## Prompt vs Loop

Use `prompt` when you want one response and no iteration state.

Use `loop` when you want WallyCode to carry state forward between iterations and keep an observable audit trail on disk.

## How the Loop Works

1. WallyCode resolves the project root, loads the saved provider from `wallycode.json`, and picks the model.
2. It creates or reopens the `.wallycode/` workspace for the current loop session.
3. If you provide a goal, WallyCode starts a new session when needed. If you omit the goal, it resumes the active session.
4. It reads the current memory documents.
5. It builds one prompt that includes the goal, loop metadata, and the current memory state.
6. It sends that prompt to `gh copilot`.
7. It saves the exact prompt, the raw provider output, and the iteration log.
8. It tries to parse the response as structured JSON.
9. If the response is plain text instead of JSON, it normalizes that text into the memory documents instead of crashing.
10. It updates `.wallycode/session.json` so the next run resumes at the next iteration number.

Use `--steps <n>` when you want more than one iteration in a single invocation.

`resume` and `continue` are accepted as aliases for `loop`.

Under the hood, the provider command is:

```text
gh copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>
```