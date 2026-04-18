# WallyCode

WallyCode is a .NET 8 CLI for one-shot repo prompts and stateful repo work through GitHub Copilot CLI.

## Requirements

- .NET 8 SDK
- GitHub CLI installed and authenticated
- a runnable `copilot` CLI

## Use It Now

Build once:

```powershell
dotnet build WallyCode.sln
```

Fastest first command:

```powershell
dotnet run --project WallyCode.Console -- prompt "Summarize this repository in one paragraph."
```

Examples below are shown as plain commands. If the executable is not on your `PATH`, prefix them with:

```powershell
dotnet run --project WallyCode.Console --
```

## Basic Commands

One-shot prompt:

```text
prompt "Summarize this repository in one paragraph."
prompt "Summarize this repository in one paragraph." --source C:\src\my-repo
```

Start a stateful run:

```text
loop "Analyze this repo, do one bounded chunk of work, and stop when the goal is complete."
```

Continue the current run:

```text
loop
```

Add input for the next iteration:

```text
respond "Use the simpler approach"
loop
```

Run multiple iterations in one invocation:

```text
loop --steps 3
```

Use a separate workspace for an isolated run:

```text
loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

Interactive shell:

```text
shell
shell --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

## Quick Setup

List providers and readiness:

```text
provider
```

Current providers:

- `gh-copilot-claude`
- `gh-copilot-gpt5`

Set the default provider for the current project:

```text
provider gh-copilot-gpt5 --set
```

List models for a provider:

```text
provider gh-copilot-gpt5 --models
```

Set the default model for the current or selected provider:

```text
provider gh-copilot-gpt5 --model gpt-5
```

Override provider or model per command:

```text
prompt "Summarize this repository" --provider gh-copilot-gpt5
prompt "Summarize this repository" --model gpt-5
loop "Work on issue 123" --provider gh-copilot-gpt5
loop "Work on issue 123" --model gpt-5
```

## Defaults

- Current directory is the source root unless you pass `--source`.
- Project defaults live in `wallycode.json`.
- Runtime data lives in `.wallycode/` unless you pass `--memory-root`.
- `respond` appends user input for the next `loop`; it does not resume automatically.