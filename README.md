# WallyCode

WallyCode is a small .NET 8 console app that wraps the GitHub Copilot CLI.

The command surface is intentionally small:

- `provider` reviews providers and sets the default provider for the repo
- `prompt` runs a one-off prompt
- `loop` starts or continues a stateful loop session
- `respond` adds a user response for the next loop iteration
- `shell` starts interactive mode so you can run the same commands repeatedly

## Requirements

- .NET 8 SDK
- GitHub CLI installed
- GitHub CLI authenticated
- a runnable `copilot` CLI

Before a prompt or loop run starts, WallyCode checks whether the selected provider is ready.

## Quick Start

Build the solution:

```powershell
dotnet build WallyCode.sln
```

Show top-level help:

```powershell
dotnet run --project .\WallyCode.Console -- --help
```

List providers and see which one is active:

```powershell
dotnet run --project .\WallyCode.Console -- provider
```

Set the default provider for this repo:

```powershell
dotnet run --project .\WallyCode.Console -- provider gh-copilot-gpt5 --set
```

Run one prompt using the saved default provider:

```powershell
dotnet run --project .\WallyCode.Console -- prompt "Summarize this repository in one short paragraph."
```

Override the provider for one prompt without changing the saved default:

```powershell
dotnet run --project .\WallyCode.Console -- prompt "Summarize this repository in one short paragraph." --provider gh-copilot-claude
```

Start a loop:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Analyze this repo, do one bounded chunk of work, update memory, and stop when the goal is complete."
```

Continue the active loop:

```powershell
dotnet run --project .\WallyCode.Console -- loop
```

Start interactive mode:

```powershell
dotnet run --project .\WallyCode.Console -- shell
```

Inside the shell, run the same commands directly:

```text
provider
provider gh-copilot-claude --set
prompt "Summarize this repository"
loop "Work on issue 123"
loop
respond "Use the simpler approach"
```

## Provider Command

Use `provider` for all provider management.

List providers:

```powershell
dotnet run --project .\WallyCode.Console -- provider
```

Set the default provider:

```powershell
dotnet run --project .\WallyCode.Console -- provider gh-copilot-claude --set
```

Current providers:

- `gh-copilot-claude`
- `gh-copilot-gpt5`

If you never set one, WallyCode defaults to `gh-copilot-claude`.

## Prompt vs Loop

Use `prompt` when you want one response and no iteration state.

Use `loop` when you want WallyCode to carry state forward between iterations and keep an audit trail on disk.

## Loop Basics

- `loop <goal>` starts a session
- `loop` continues the active session
- `loop --steps <n>` runs more than one iteration in one invocation
- `respond "..."` stores user input for the next loop iteration

Use a separate memory folder when you want an isolated session:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

## What WallyCode Writes

WallyCode stores project settings in `wallycode.json` at the repo root.

Loop runs write state under `.wallycode/`:

- `.wallycode/session.json`
- `.wallycode/memory/goal.md`
- `.wallycode/memory/current-tasks.md`
- `.wallycode/memory/perspectives.md`
- `.wallycode/memory/next-steps.md`
- `.wallycode/memory/current-state.md`
- `.wallycode/prompts/iteration-###.txt`
- `.wallycode/raw/iteration-###.txt`
- `.wallycode/logs/iteration-###.md`
- `.wallycode/logs/session.log`

Prompt runs also write files under `.wallycode/`:

- `.wallycode/prompts/prompt-*.txt`
- `.wallycode/raw/prompt-*.txt`
- `.wallycode/logs/prompt-*.log`

## Runtime Command

Under the hood, WallyCode runs:

```text
copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>