# WallyCode

WallyCode is a small .NET 8 console app that wraps the GitHub Copilot CLI.

The command surface is intentionally small:

- `provider` reviews providers, lists provider models, and sets the default provider for the repo
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

```text
--help
```

List providers and see which one is active:

```text
provider
```

List the models for the current default provider:

```text
provider --models
```

List the models for one provider:

```text
provider gh-copilot-claude --models
```

Set the default provider for this repo:

```text
provider gh-copilot-gpt5 --set
```

Run one prompt using the saved default provider:

```text
prompt "Summarize this repository in one short paragraph."
```

Override the provider for one prompt without changing the saved default:

```text
prompt "Summarize this repository in one short paragraph." --provider gh-copilot-claude
```

Start a loop:

```text
loop "Analyze this repo, do one bounded chunk of work, update memory, and stop when the goal is complete."
```

Continue the active loop:

```text
loop
```

Start interactive mode:

```text
shell
```

Inside the shell, run the same commands directly:

```text
provider
provider --models
provider gh-copilot-claude --models
provider gh-copilot-claude --set
prompt "Summarize this repository"
loop "Work on issue 123"
loop
respond "Use the simpler approach"
```

## Provider Command

Use `provider` for all provider management.

List providers:

```text
provider
```

List models for the current default provider:

```text
provider --models
```

List models for one provider:

```text
provider gh-copilot-claude --models
```

Set the default provider:

```text
provider gh-copilot-claude --set
```

For the GitHub Copilot provider, `provider --models` queries the GitHub Copilot model catalog using your authenticated GitHub CLI session and marks the provider's default model.

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

```text
loop "Work on issue 123" --memory-root .\.wallycode-issue-123
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
copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>```