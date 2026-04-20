# WallyCode

WallyCode is a routing-driven CLI for running GitHub Copilot workflows against a repo.

The core idea is simple:
- use `loop` as the main routed workflow command
- use `ask` as a shortcut for `loop --definition ask`
- use `act` as a shortcut for `loop --definition act`

## Quick Start

Initialize the workspace once before normal day-to-day commands:

```text
wallycode setup
```

Use `loop --definition ask` when you want a direct answer with no file changes:

```text
loop "Summarize this repository in one short paragraph." --definition ask
```

Use `loop --definition act` when you want direct execution in the repo plus a normal response:

```text
loop "Implement a minimal health-check endpoint." --definition act
```

Use `loop` with the default routed workflow when the task needs iteration, memory, and staged progress:

```text
loop "Build a simple browser-based tic-tac-toe game in this repo."
```

## Tutorials

Readme-style walkthroughs live in [WallyCode.Console/Tutorials](WallyCode.Console/Tutorials).

- [WallyCode.Console/Tutorials/README.md](WallyCode.Console/Tutorials/README.md) - tutorial index and usage notes.
- [WallyCode.Console/Tutorials/book-story.md](WallyCode.Console/Tutorials/book-story.md) - use `act` style workflows to build and revise a story in markdown files.
- [WallyCode.Console/Tutorials/repo-review.md](WallyCode.Console/Tutorials/repo-review.md) - use `ask` style workflows to review a repository without changing files.
- [WallyCode.Console/Tutorials/tic-tac-toe.md](WallyCode.Console/Tutorials/tic-tac-toe.md) - use the routed loop to build a small game step by step.

The `tutorial` command is intended to list these guides and print one by name:

```text
tutorial --list
tutorial repo-review
tutorial book-story
tutorial tic-tac-toe
```

Continue the active loop:

```text
loop
```

Run several iterations at once:

```text
loop --steps 3
```

Answer the loop when it asks you something:

```text
respond "Use the simpler approach."
```

Later, configure providers and default models for smoother day-to-day use:

```text
provider
provider --models
provider gh-copilot-gpt5 --set
provider gh-copilot-gpt5 --model gpt-5
```

That is the natural progression: use `loop` for routed work, use the `ask` and `act` shortcuts when they fit, and configure providers once you know which model setup you want as your default.

## Routing Definitions

`loop` runs the routing engine. The first call starts a session. Later calls continue it.

Pick a different routing definition:

```text
loop "Build the export feature." --definition full-pipeline
```

Shipped definitions (in [WallyCode.Console/Routing/Definitions](WallyCode.Console/Routing/Definitions)):

- `ask` - single-step routed definition for direct question-answering. Intended to behave like a normal LLM response with no file changes.
- `act` - single-step routed definition for direct execution. Intended to allow file changes and then return a normal user-facing response.
- `requirements` (default) - collect_requirements -> produce_tasks
- `tasks` - produce_tasks -> execute_tasks
- `full-pipeline` - collect_requirements -> produce_tasks -> execute_tasks

`ask` and `act` are intentionally simple routing definitions. They do not route across multiple logical units. The `ask` and `act` commands are convenience aliases for these definitions, but the underlying routed model is still `loop`.

Point at a different repo or store session state somewhere else:

```text
loop "Work on issue 123" --source C:\src\my-repo --memory-root C:\temp\wallycode-session
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
wallycode loop --definition ask "Summarize this repository in one short paragraph."
```

## Other Commands

Shortcut commands for the common single-step routed workflows:

```text
ask "Summarize this repository in one short paragraph."
ask "Summarize this repository." --source C:\src\my-repo
```

Equivalent `loop` form:

```text
loop --definition ask "Summarize this repository in one short paragraph."
loop --definition ask "Summarize this repository." --source C:\src\my-repo
```

Use `loop` directly when you want the explicit routed form. Use `ask` and `act` when you want the shorter command shape.

Interactive shell that keeps `--source` and `--memory-root` defaults for every command run inside it:

```text
shell
shell --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

Inside the shell:

```text
loop --definition ask "Explain the architecture of this repo."
loop --definition act "Implement a minimal health-check endpoint."
loop "Work on issue 123"
loop
respond "Use the simpler approach"
exit
```

Built-in walkthrough:

```text
tutorial
```

## Providers and Models

WallyCode saves a default provider and optional model in `wallycode.json` at the project root.

```text
provider
provider --models
provider gh-copilot-gpt5 --set
provider --model gpt-5
```

Available providers:

- `gh-copilot-claude` (default)
- `gh-copilot-gpt5`

Override for a single run:

```text
ask "Summarize this repository" --provider gh-copilot-gpt5
loop --definition ask "Summarize this repository" --provider gh-copilot-gpt5
loop "Build the export feature." --model gpt-5
```

## Files Written

- `wallycode.json` - project settings (default provider, model).
- `.wallycode/` - loop session state. Override location with `--memory-root`.

`source` is the folder the provider operates against. `memory-root` is where WallyCode stores session data.

## Working Against Another Repo

The easiest way to have one `wallycode` executable operate on another repo or folder is to pass `--source`.

```text
wallycode loop --definition ask "Summarize this repository." --source C:\src\repo-a
wallycode loop --definition act "Implement a minimal health-check endpoint." --source C:\src\repo-a
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
wallycode loop --definition ask "Summarize this repository in one short paragraph." --source C:\src\my-repo
wallycode loop --definition act "Add a README section for local development." --source C:\src\my-repo
wallycode loop "Build tic-tac-toe." --source C:\src\my-repo
```

## Remote Workspaces

The executable location and the workspace WallyCode operates on are separate concerns.

- Install `wallycode` wherever it is convenient to run.
- Use `--source` to point at the repo or folder you want to work on.
- `wallycode.json` stays in the source workspace.
- `.wallycode/` stays in the source workspace by default unless `--memory-root` is provided.

That model already allows one installed executable to work against different repos. Installation stays manual: place the published build where you want it, then run setup there or against the workspace you want to initialize.

