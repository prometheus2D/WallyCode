# WallyCode

WallyCode is a routing-driven CLI for running GitHub Copilot workflows against a repo.

The core idea is simple:
- use `ask` for direct answers with no file changes
- use `act` for direct repo work with a normal response
- use `loop` when the task needs iteration, memory, and staged progress

## Quick Start

Start with `ask` when you want a direct answer with no file changes:

```text
loop "Summarize this repository in one short paragraph." --definition ask
```

Move to `act` when you want direct execution in the repo plus a normal response:

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

Tutorial automation and regression guidance lives here:

- [WallyCode.Console/Docs/tutorial-automation-process.md](WallyCode.Console/Docs/tutorial-automation-process.md) - use tutorials as executable scenarios with mock-provider tests and live runs.

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

That is the natural progression: `ask` for direct answers, `act` for direct repo changes, `loop` for multi-step routed work, and provider configuration once you know which model setup you want as your default.

## Routing Definitions

`loop` runs the routing engine. The first call starts a session. Later calls continue it.

Pick a different routing definition:

```text
loop "Build the export feature." --definition full-pipeline
```

Shipped definitions (in [WallyCode.Console/Routing/Definitions](WallyCode.Console/Routing/Definitions)):

- `ask` - single prompt unit for direct question-answering. Intended to behave like a normal LLM response with no file changes.
- `act` - single prompt unit for direct execution. Intended to allow file changes and then return a normal user-facing response.
- `requirements` (default) - collect_requirements -> produce_tasks
- `tasks` - produce_tasks -> execute_tasks
- `full-pipeline` - collect_requirements -> produce_tasks -> execute_tasks

`ask` and `act` are intentionally simple routing definitions. They do not route across multiple logical units. They exist as baseline workflows that fit into the same routing-definition model as the more structured pipelines, while leaving room for richer routing behavior later.

Point at a different repo or store session state somewhere else:

```text
loop "Work on issue 123" --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

Session state is written under `.wallycode/` in the project root by default.

## Other Commands

One-shot prompt, no session, no memory:

```text
prompt "Summarize this repository in one short paragraph."
prompt "Summarize this repository." --source C:\src\my-repo
```

If you want the same kind of direct interaction expressed through the routing-definition system, start with `loop --definition ask`, move to `loop --definition act` when file changes are needed, and use the default `loop` workflow when the task needs iteration.

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
prompt "Summarize this repository" --provider gh-copilot-gpt5
loop "Build the export feature." --model gpt-5
```

## Files Written

- `wallycode.json` - project settings (default provider, model).
- `.wallycode/` - session state, prompts, raw output, and logs. Override location with `--memory-root`.

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
- `--memory-root` selects where session state, logs, prompts, and raw output are stored
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

That model already allows one installed executable to work against different repos. The setup work that remains is making installation and self-copy simple enough that a published build can be moved around with minimal friction.

## Design Docs

Background on the routing model lives under [WallyCode.Console/Docs](WallyCode.Console/Docs).

- [WallyCode.Console/Docs/setup-and-remote-workspaces.md](WallyCode.Console/Docs/setup-and-remote-workspaces.md) - proposed setup flow, self-copy behavior, and remote workspace model.
- [WallyCode.Console/Docs/tutorial-automation-process.md](WallyCode.Console/Docs/tutorial-automation-process.md) - tutorial automation process for mock-provider regression tests and live scenario runs.
