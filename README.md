# WallyCode

WallyCode is a small .NET 8 console app for running GitHub Copilot CLI prompts against a repo.

## Requirements

- .NET 8 SDK
- GitHub CLI installed and authenticated
- a runnable `copilot` CLI

## Quick Start

Build:

```powershell
dotnet build WallyCode.sln
```

Show the built-in walkthrough:

```text
tutorial
```

Run a prompt in the current repo:

```text
prompt "Summarize this repository in one short paragraph."
```

Run a prompt against a different folder:

```text
prompt "Summarize this repository in one short paragraph." --source C:\src\my-repo
```

That is the fastest way to get started.

## Design Docs

The current CLI still uses the `loop` command name.

The future-state design documents under `WallyCode.Console/Docs/` intentionally use `routing`, `definition`, `logical unit`, `session`, and `run` terminology instead of modeling the system as loops.

Core design docs:

- `WallyCode.Console/Docs/routing.md`
- `WallyCode.Console/Docs/routing-examples.md`
- `WallyCode.Console/Docs/routing-testing.md`
- `WallyCode.Console/Docs/requirements-gathering.md`
- `WallyCode.Console/Docs/task-generation.md`
- `WallyCode.Console/Docs/task-execution.md`

## Routed Sessions

`route` runs the routing engine against a routing definition.

Start a new routed session using the default `requirements` definition:

```text
route "Clarify export requirements for the reporting tool."
```

Use a different shipped definition:

```text
route "Build the export feature." --definition full-pipeline
```

Continue the active routed session:

```text
route
```

Run several iterations in one invocation:

```text
route --steps 3
```

Answer an `[ASK_USER]` stop and immediately resume:

```text
route-respond "Exports should support csv and pdf."
```

Store a response without resuming:

```text
route-respond "Waiting on more info." --no-resume
```

Routed sessions are stored by default under `.wallycode/route/`. Override with `--memory-root`.

Shipped routing definitions live under `WallyCode.Console/Routing/Definitions/`:

- `requirements` - collect_requirements -> produce_tasks
- `tasks` - produce_tasks -> execute_tasks
- `execute` - execute_tasks only
- `full-pipeline` - collect_requirements -> produce_tasks -> execute_tasks

## Current CLI Stateful Runs

Use `loop` when you want WallyCode to keep state between iterations.

Start a session:

```text
loop "Analyze this repo, do one bounded chunk of work, update memory, and stop when the goal is complete."
```

Continue the active session:

```text
loop
```

Add user input for the next run:

```text
respond "Use the simpler approach"
```

Run multiple iterations in one invocation:

```text
loop --steps 3
```

Use a separate memory folder for an isolated session:

```text
loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

## Simple Tutorial: Tic-Tac-Toe

Example prompt-first workflow:

1. Create or open a repo for the sample.
2. Run a prompt that asks Copilot to build the first version.
3. Review the output.
4. If you want iterative progress, switch to the current `loop` command.

One-shot prompt example:

```text
prompt "Create a simple browser-based tic-tac-toe game in this repo. Keep it minimal: HTML, CSS, and JavaScript only. Add clear file-by-file steps and then implement the code."
```

Iterative example using the current `loop` command:

```text
loop "Build a simple browser-based tic-tac-toe game in this repo. Do one small bounded chunk per iteration, keep the implementation minimal, and stop when the game is complete."
```

Then continue the session as needed:

```text
loop
respond "Now polish the UI a little, but keep it simple."
```

## Shell

Start interactive mode:

```text
shell
```

Start a shell with a specific source and memory root:

```text
shell --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

When you start the shell this way, commands entered inside it inherit those defaults unless you override them explicitly.

Inside the shell, run the same commands directly:

```text
prompt "Summarize this repository"
loop "Work on issue 123"
loop
respond "Use the simpler approach"
exit
```

## Providers and Models

WallyCode saves a default provider and optional model in `wallycode.json` at the project root.

Useful commands:

```text
provider
provider --models
provider gh-copilot-gpt5 --set
provider --model gpt-5
```

Current providers:

- `gh-copilot-claude` default
- `gh-copilot-gpt5`

If you do nothing, WallyCode uses `gh-copilot-claude`.

You can also override provider or model for a single prompt or for a new session started with `loop`:

```text
prompt "Summarize this repository" --provider gh-copilot-gpt5
prompt "Summarize this repository" --model gpt-5
```

## Files Written

WallyCode stores project settings in `wallycode.json`.

`source` is the folder WallyCode and the provider operate against.

`memory-root` is the folder where WallyCode stores session memory, prompts, raw output, logs, and session state.

Runtime files are written under `.wallycode/` by default, or under the folder passed to `--memory-root`.