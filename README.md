# WallyCode

WallyCode is a .NET 8 console app that drives GitHub Copilot CLI through a routing engine. You give it a goal, and it runs one or more iterations of work against a repo, moving through named units (collect requirements, produce tasks, execute tasks) until it finishes or needs your input.

## Requirements

- .NET 8 SDK
- GitHub CLI installed and authenticated
- a runnable `copilot` CLI

## Quick Start

Build:

```powershell
dotnet build WallyCode.sln
```

Start a new loop against the current folder:

```text
loop "Build a simple browser-based tic-tac-toe game in this repo."
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

That is the full basic workflow.

## Loop Command

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

If you want the same kind of direct interaction expressed through the routing-definition system, use `loop --definition ask` or `loop --definition act` when those workflows are appropriate.

Interactive shell that keeps `--source` and `--memory-root` defaults for every command run inside it:

```text
shell
shell --source C:\src\my-repo --memory-root C:\temp\wallycode-session
```

Inside the shell:

```text
prompt "Summarize this repository"
loop "Work on issue 123"
loop --definition ask "Explain the architecture of this repo."
loop --definition act "Implement a minimal health-check endpoint."
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

## Design Docs

Background on the routing model lives under [WallyCode.Console/Docs](WallyCode.Console/Docs).