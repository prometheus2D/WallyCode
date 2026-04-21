# WallyCode

WallyCode is a routing-driven CLI for running GitHub Copilot workflows against a repo.

Recommended progression:
- `ask` - direct answer, no file changes
- `act` - direct execution plus a normal response
- `loop` - iterative routed workflow with memory and staged progress

Aliases:
- `ask` = `loop --definition ask`
- `act` = `loop --definition act`
- `loop` without `--definition` uses the default routed workflow: `requirements`

## Quick Start

Initialize once, verify provider/model, then start with `ask`, `act`, and `loop` as needed:

```text
wallycode setup
wallycode provider
wallycode provider --models
wallycode ask "Summarize this repository in one short paragraph."
wallycode act "Implement a minimal health-check endpoint."
wallycode loop "Build a simple browser-based tic-tac-toe game in this repo."
```

If needed, set the default provider and model explicitly:

```text
wallycode provider gh-copilot-claude --set
wallycode provider --model claude-sonnet-4
wallycode provider gh-copilot-gpt5 --set
wallycode provider --model gpt-5
```

## Provider and Model Configuration

WallyCode stores workspace configuration in `wallycode.json`.

Check the current provider:

```text
wallycode provider
```

This lists providers, shows which one is the default, and shows the current model for each provider entry.

Check the current model for the active provider:

```text
wallycode provider --models
```

This lists models for the active provider and marks the default model.

Check models for a specific provider:

```text
wallycode provider gh-copilot-claude --models
wallycode provider gh-copilot-gpt5 --models
```

Refresh and persist discovered models for a provider:

```text
wallycode provider gh-copilot-claude --refresh
wallycode provider gh-copilot-gpt5 --refresh
```

Set defaults:

```text
wallycode provider gh-copilot-claude --set
wallycode provider gh-copilot-gpt5 --set
wallycode provider --model claude-sonnet-4
wallycode provider --model gpt-5
```

Available built-in providers:
- `gh-copilot-claude` (default)
- `gh-copilot-gpt5`

If commands are not behaving as expected, check provider and model configuration first.

## Global Prompt

If you want a workspace-wide prompt added to every routed logical-unit prompt, put it in `wallycode.json` as `globalPrompt`.

Example:

```json
{
  "provider": "gh-copilot-claude",
  "model": "claude-sonnet-4",
  "globalPrompt": "selectedKeyword must exactly match one of the allowed keywords as written, including brackets. Output JSON only."
}
```

Notes:
- `globalPrompt` is optional
- if `globalPrompt` is not set, no global prompt is injected
- this applies to routed prompts used by `ask`, `act`, and `loop`
- this is a workspace-level setting, so it belongs in the repo's `wallycode.json`

## Core Commands

### ask
Use for inspection, explanation, summarization, and review without file changes.

```text
wallycode ask "Explain the architecture of this repo."
wallycode ask "Summarize this repository." --source C:\src\my-repo
```

Equivalent routed form:

```text
wallycode loop --definition ask "Explain the architecture of this repo."
```

### act
Use for focused direct execution in the repo plus a normal response.

```text
wallycode act "Implement a minimal health-check endpoint."
wallycode act "Add a README section for local development." --source C:\src\my-repo
```

Equivalent routed form:

```text
wallycode loop --definition act "Implement a minimal health-check endpoint."
```

### loop
Use when the task needs iteration, memory, and staged progress.

```text
wallycode loop "Build tic-tac-toe in this repo."
wallycode loop
wallycode loop --steps 3
wallycode respond "Use the simpler approach."
```

Point at another repo or move session state elsewhere:

```text
wallycode loop "Work on issue 123" --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

Session state is written under `.wallycode/` in the project root by default.

## Routing Definitions

`loop` starts or continues a routed session.

Shipped definitions:
- `ask` - single-step direct answer, no file changes
- `act` - single-step direct execution
- `requirements` (default) - `collect_requirements -> produce_tasks`
- `tasks` - `produce_tasks -> execute_tasks`
- `full-pipeline` - `collect_requirements -> produce_tasks -> execute_tasks`

Pick a definition explicitly:

```text
wallycode loop "Build the export feature." --definition full-pipeline
```

`ask` and `act` are intentionally simple routed definitions. They are convenience commands over `loop`.

## Setup

Use `setup` once per workspace:

```text
wallycode setup
wallycode setup --directory C:\src\my-repo
wallycode setup --force
wallycode setup --vs-build
```

Typical flow:

```text
wallycode setup --directory C:\src\my-repo
cd C:\src\my-repo
wallycode provider
wallycode provider --models
wallycode ask "Summarize this repository in one short paragraph."
```

## Shell

Interactive shell keeps `--source` and `--memory-root` defaults for commands run inside it:

```text
wallycode shell
wallycode shell --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

Inside the shell:

```text
ask "Explain the architecture of this repo."
act "Implement a minimal health-check endpoint."
loop "Work on issue 123"
loop
respond "Use the simpler approach"
exit
```

## Working Against Another Repo

Use `--source` to operate on another repo or folder:

```text
wallycode ask "Summarize this repository." --source C:\src\repo-a
wallycode act "Implement a minimal health-check endpoint." --source C:\src\repo-a
wallycode loop "Build tic-tac-toe." --source D:\projects\demo-app
```

Use `--memory-root` only when you want runtime state somewhere other than the source repo's default `.wallycode` folder:

```text
wallycode loop "Work on issue 123" --source C:\src\repo-a --memory-root C:\temp\repo-a-session
```

Meaning:
- `--source` selects the repo or folder WallyCode operates on
- `--memory-root` selects where loop session state is stored
- neither changes where the executable is installed

Typical remote-workspace flow:

```text
wallycode provider --source C:\src\my-repo
wallycode provider --models --source C:\src\my-repo
wallycode ask "Summarize this repository in one short paragraph." --source C:\src\my-repo
wallycode act "Add a README section for local development." --source C:\src\my-repo
wallycode loop "Build tic-tac-toe." --source C:\src\my-repo
```

## Files Written

- `wallycode.json` - workspace settings, including default provider/model, optional `globalPrompt`, and provider catalog metadata
- `.wallycode/` - loop session state; override with `--memory-root`

`source` is the folder the provider operates against. `memory-root` is where WallyCode stores session data.

## Tutorials

Walkthroughs live in [WallyCode.Console/Tutorials](WallyCode.Console/Tutorials):
- [WallyCode.Console/Tutorials/README.md](WallyCode.Console/Tutorials/README.md) - tutorial index and usage notes
- [WallyCode.Console/Tutorials/book-story.md](WallyCode.Console/Tutorials/book-story.md) - `act` workflow on markdown files
- [WallyCode.Console/Tutorials/repo-review.md](WallyCode.Console/Tutorials/repo-review.md) - `ask` workflow without file changes
- [WallyCode.Console/Tutorials/tic-tac-toe.md](WallyCode.Console/Tutorials/tic-tac-toe.md) - routed `loop` workflow

Use:

```text
wallycode tutorial --list
wallycode tutorial repo-review
wallycode tutorial book-story
wallycode tutorial tic-tac-toe
```

## Remote Workspaces

Executable location and workspace location are separate:
- install `wallycode` wherever convenient
- use `--source` to point at the repo or folder to operate on
- `wallycode.json` stays in the source workspace
- `.wallycode/` stays in the source workspace unless `--memory-root` is provided

One installed executable can operate against multiple repos.

