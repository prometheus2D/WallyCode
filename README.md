# WallyCode

WallyCode is a routing-driven CLI for running GitHub Copilot workflows against a repo.

The recommended progression is:
- use `ask` for direct answers with no file changes
- use `act` for direct execution plus a normal response
- use `loop` when the task needs iteration, memory, and staged progress

`ask` is a shortcut for `loop --definition ask`.
`act` is a shortcut for `loop --definition act`.
`loop` without `--definition` uses the default routed workflow.

## Quick Start

Initialize the workspace once:

```text
wallycode setup
```

Then verify or configure the provider and model you want to use:

```text
wallycode provider
wallycode provider --models
wallycode provider gh-copilot-claude --set
wallycode provider --model claude-sonnet-4
```

Start with `ask` when you want a direct answer with no file changes:

```text
wallycode ask "Summarize this repository in one short paragraph."
```

Use `act` when you want direct execution in the repo plus a normal response:

```text
wallycode act "Implement a minimal health-check endpoint."
```

Use `loop` when the task needs iteration, memory, and staged progress:

```text
wallycode loop "Build a simple browser-based tic-tac-toe game in this repo."
```

## Recommended Day-to-Day Flow

For a new workspace:

```text
wallycode setup
wallycode provider
wallycode provider --models
```

If needed, set the default provider and model explicitly:

```text
wallycode provider gh-copilot-gpt5 --set
wallycode provider --model gpt-5
```

Then use commands in this order:

1. `ask` - inspect, summarize, explain, review
2. `act` - make a focused change directly
3. `loop` - run a multi-step routed workflow with memory

Examples:

```text
wallycode ask "Explain the architecture of this repo."
wallycode act "Add a README section for local development."
wallycode loop "Build tic-tac-toe in this repo."
```

## Routing Definitions

`loop` runs the routing engine. The first call starts a session. Later calls continue it.

Pick a different routing definition:

```text
wallycode loop "Build the export feature." --definition full-pipeline
```

Shipped definitions (in [WallyCode.Console/Routing/Definitions](WallyCode.Console/Routing/Definitions)):

- `ask` - single-step routed definition for direct question-answering. Intended to behave like a normal LLM response with no file changes.
- `act` - single-step routed definition for direct execution. Intended to allow file changes and then return a normal user-facing response.
- `requirements` (default) - collect_requirements -> produce_tasks
- `tasks` - produce_tasks -> execute_tasks
- `full-pipeline` - collect_requirements -> produce_tasks -> execute_tasks

`ask` and `act` are intentionally simple routing definitions. They do not route across multiple logical units. The `ask` and `act` commands are convenience aliases for these definitions, but the underlying routed model is still `loop`.

Continue the active loop:

```text
wallycode loop
```

Run several iterations at once:

```text
wallycode loop --steps 3
```

Answer the loop when it asks you something:

```text
wallycode respond "Use the simpler approach."
```

Point at a different repo or store session state somewhere else:

```text
wallycode loop "Work on issue 123" --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

Session state is written under `.wallycode/` in the project root by default.

## Setup

Use `setup` once per workspace you want WallyCode to initialize.

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

## Other Commands

Shortcut commands for the common single-step routed workflows:

```text
wallycode ask "Summarize this repository in one short paragraph."
wallycode ask "Summarize this repository." --source C:\src\my-repo
wallycode act "Implement a minimal health-check endpoint."
```

Equivalent `loop` forms:

```text
wallycode loop --definition ask "Summarize this repository in one short paragraph."
wallycode loop --definition ask "Summarize this repository." --source C:\src\my-repo
wallycode loop --definition act "Implement a minimal health-check endpoint."
```

Use `loop` directly when you want the explicit routed form. Use `ask` and `act` when you want the shorter command shape.

Interactive shell that keeps `--source` and `--memory-root` defaults for every command run inside it:

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

## Providers and Models

WallyCode saves a default provider and optional model in `wallycode.json` at the project root.

Check the current configuration:

```text
wallycode provider
wallycode provider --models
```

Set the default provider:

```text
wallycode provider gh-copilot-claude --set
wallycode provider gh-copilot-gpt5 --set
```

Set the default model for the current provider:

```text
wallycode provider --model claude-sonnet-4
wallycode provider --model gpt-5
```

Available providers:

- `gh-copilot-claude` (default)
- `gh-copilot-gpt5`

Override for a single run:

```text
wallycode ask "Summarize this repository" --provider gh-copilot-gpt5
wallycode act "Implement a minimal health-check endpoint." --provider gh-copilot-gpt5
wallycode loop "Build the export feature." --model gpt-5
```

If commands are not behaving as expected, check provider and model configuration first.

## Files Written

- `wallycode.json` - project settings (default provider, model).
- `.wallycode/` - loop session state. Override location with `--memory-root`.

`source` is the folder the provider operates against. `memory-root` is where WallyCode stores session data.

## Working Against Another Repo

The easiest way to have one `wallycode` executable operate on another repo or folder is to pass `--source`.

```text
wallycode ask "Summarize this repository." --source C:\src\repo-a
wallycode act "Implement a minimal health-check endpoint." --source C:\src\repo-a
wallycode loop "Build tic-tac-toe." --source D:\projects\demo-app
```

Use `--memory-root` only when you want the runtime workspace somewhere other than the source repo's default `.wallycode` folder.

```text
wallycode loop "Work on issue 123" --source C:\src\repo-a --memory-root C:\temp\repo-a-session
```

Meaning:
- `--source` selects the repo or folder WallyCode operates on
- `--memory-root` selects where loop session state is stored
- neither option changes where the executable itself is installed

Typical remote-workspace flow:

```text
wallycode provider --source C:\src\my-repo
wallycode provider --models --source C:\src\my-repo
wallycode ask "Summarize this repository in one short paragraph." --source C:\src\my-repo
wallycode act "Add a README section for local development." --source C:\src\my-repo
wallycode loop "Build tic-tac-toe." --source C:\src\my-repo
```

## Tutorials

Readme-style walkthroughs live in [WallyCode.Console/Tutorials](WallyCode.Console/Tutorials).

- [WallyCode.Console/Tutorials/README.md](WallyCode.Console/Tutorials/README.md) - tutorial index and usage notes.
- [WallyCode.Console/Tutorials/book-story.md](WallyCode.Console/Tutorials/book-story.md) - use `act` style workflows to build and revise a story in markdown files.
- [WallyCode.Console/Tutorials/repo-review.md](WallyCode.Console/Tutorials/repo-review.md) - use `ask` style workflows to review a repository without changing files.
- [WallyCode.Console/Tutorials/tic-tac-toe.md](WallyCode.Console/Tutorials/tic-tac-toe.md) - use the routed loop to build a small game step by step.

The `tutorial` command is intended to list these guides and print one by name:

```text
wallycode tutorial --list
wallycode tutorial repo-review
wallycode tutorial book-story
wallycode tutorial tic-tac-toe
```

## Remote Workspaces

The executable location and the workspace WallyCode operates on are separate concerns.

- Install `wallycode` wherever it is convenient to run.
- Use `--source` to point at the repo or folder you want to work on.
- `wallycode.json` stays in the source workspace.
- `.wallycode/` stays in the source workspace by default unless `--memory-root` is provided.

That model already allows one installed executable to work against different repos. Installation stays manual: place the published build where you want it, then run setup there or against the workspace you want to initialize.

