# WallyCode

WallyCode is a small .NET 8 console app that wraps the GitHub Copilot CLI.

The command surface is intentionally small:

- `provider` lists providers, lists provider models, and sets the default provider and model for the current project
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

List providers and see which one is default:

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

Set the default provider for this project:

```text
provider gh-copilot-gpt5 --set
```

Set the default model for the current default provider:

```text
provider --model gpt-5
```

Set the default model for a specific provider:

```text
provider gh-copilot-claude --model claude-sonnet-4
```

Run one prompt using the saved default provider and model:

```text
prompt "Summarize this repository in one short paragraph."
```

Override the provider for one prompt without changing the saved default:

```text
prompt "Summarize this repository in one short paragraph." --provider gh-copilot-claude
```

Override the model for one prompt without changing the saved default:

```text
prompt "Summarize this repository in one short paragraph." --model gpt-5
```

Start a loop:

```text
loop "Analyze this repo, do one bounded chunk of work, update memory, and stop when the goal is complete."
```

Continue the active loop:

```text
loop
```

Run multiple loop iterations in one invocation:

```text
loop --steps 3
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
provider --model gpt-5
provider gh-copilot-claude --model claude-sonnet-4
prompt "Summarize this repository"
loop "Work on issue 123"
loop --steps 2
loop
respond "Use the simpler approach"
reset-memory
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

Set the default model for the current default provider:

```text
provider --model gpt-5
```

Set the default model for a specific provider:

```text
provider gh-copilot-claude --model claude-sonnet-4
```

Typical flow:

```text
provider
provider --models
provider --model gpt-5
```

Or for a specific provider:

```text
provider gh-copilot-claude --models
provider gh-copilot-claude --model claude-sonnet-4
```

For the GitHub Copilot providers, `provider --models` queries the GitHub Copilot model catalog using your authenticated GitHub CLI session and marks the saved default model.

Current providers:

- `gh-copilot-claude` default model `claude-sonnet-4`
- `gh-copilot-gpt5` default model `gpt-5`

If you never set one, WallyCode defaults to `gh-copilot-claude`.

Project settings are stored in `wallycode.json` at the project root.

## Prompt vs Loop

Use `prompt` when you want one response and no iteration state.

Use `loop` when you want WallyCode to carry state forward between iterations and keep an audit trail on disk.

`prompt` and new `loop` sessions use the saved default provider and model unless you override them on the command line.

## Loop Basics

- `loop <goal>` starts a session
- `loop` continues the active session
- `loop --steps <n>` runs more than one iteration in one invocation
- `loop --template <id>` starts a new session with a specific loop template
- `respond "..."` stores user input for the next loop iteration

Use a separate memory folder when you want an isolated session:

```text
loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

Use a specific source folder when the project root is not the current directory:

```text
loop "Work on issue 123" --source C:\src\my-repo
```

A loop session locks in its provider, model, template, source path, and memory root. To change those for a different run, start a separate session with a different `--memory-root`.

## Loop Templates

Built-in loop templates are loaded from `Templates/Loops/<template>.json`.

Current built-in template:

- `default`

If you omit `--template`, WallyCode uses `default`.

## Shell

Start the interactive shell:

```text
shell
```

Optional shell flags:

- `shell --source <path>`
- `shell --memory-root <path>`
- `shell --reset-memory`

Inside the shell:

- run any normal WallyCode command without the executable name
- type `reset-memory` to clear the current memory workspace
- type `exit` to quit

## What WallyCode Writes

WallyCode stores project settings in `wallycode.json` at the project root.

Loop runs write state under `.wallycode/` by default, or under the folder passed to `--memory-root`:

- `session.json`
- `memory/goal.md`
- `memory/current-tasks.md`
- `memory/perspectives.md`
- `memory/next-steps.md`
- `memory/current-state.md`
- `prompts/iteration-###.txt`
- `raw/iteration-###.txt`
- `logs/iteration-###.md`
- `logs/session.log`

Prompt runs also write files under the runtime workspace:

- `prompts/prompt-*.txt`
- `raw/prompt-*.txt`
- `logs/prompt-*.log`

## Runtime Command

Under the hood, WallyCode runs the selected provider through the GitHub Copilot CLI and passes the resolved model, source path, and prompt text.