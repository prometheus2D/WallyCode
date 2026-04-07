# WallyCode

WallyCode is a .NET 8 console app that wraps `gh copilot` for two modes:

- `prompt`: one-shot response
- `loop`: stateful iterative execution with on-disk memory

## Requirements

- .NET 8 SDK
- GitHub CLI installed
- GitHub CLI authenticated
- `gh copilot` available

## Fast Start

From the repo root:

```powershell
dotnet build WallyCode.sln
```

```powershell
dotnet run --project .\WallyCode.Console -- providers
```

```powershell
dotnet run --project .\WallyCode.Console -- set-provider gh-copilot-claude
```

One-shot use:

```powershell
dotnet run --project .\WallyCode.Console -- prompt "Summarize this repository in one short paragraph."
```

Start a new loop workspace:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Analyze this repo, do one bounded chunk of work, refresh memory, and stop when the goal is complete."
```

Continue the active loop:

```powershell
dotnet run --project .\WallyCode.Console -- loop
```

Run multiple iterations in one invocation:

```powershell
dotnet run --project .\WallyCode.Console -- loop --steps 3
```

## Mental Model

- `prompt` = one response, no iterative state
- `loop <goal>` = start a new session
- `loop` = continue the active session
- `--memory-root` = use a separate workspace
- `--model` = one-off model override

There is no separate `resume` or `continue` command.

## Commands

Show help:

```powershell
dotnet run --project .\WallyCode.Console -- --help
```

Show command help:

```powershell
dotnet run --project .\WallyCode.Console -- help <command>
```

List providers:

```powershell
dotnet run --project .\WallyCode.Console -- providers
```

Set the saved default provider for the repo:

```powershell
dotnet run --project .\WallyCode.Console -- set-provider gh-copilot-claude
```

Override the model for one run:

```powershell
dotnet run --project .\WallyCode.Console -- prompt "Summarize this repository." --model gpt-5
```

Use a different source path:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Work on issue 123" --source C:\path\to\repo
```

Use a separate memory workspace:

```powershell
dotnet run --project .\WallyCode.Console -- loop "Work on issue 123" --memory-root .\.wallycode-issue-123
```

## Workspace Architecture

Project settings are stored at the repo root:

```plaintext
wallycode.json
```

Loop state is stored under the workspace root. Default:

```plaintext
<repo>/.wallycode/
```

Layout:

```plaintext
.wallycode/
  session.json
  memory/
    goal.md
    current-tasks.md
    perspectives.md
    next-steps.md
    current-state.md
  logs/
    session.log
    iteration-001.md
    iteration-002.md
    ...
  prompts/
    iteration-001.txt
    iteration-002.txt
    ...
  raw/
    iteration-001.txt
    iteration-002.txt
    ...
```

## File Roles

- `session.json`: active session metadata
- `memory/goal.md`: original goal
- `memory/current-tasks.md`: current working task list
- `memory/perspectives.md`: persistent guidance
- `memory/next-steps.md`: next actions
- `memory/current-state.md`: latest summarized state
- `logs/session.log`: session console log
- `logs/iteration-###.md`: normalized iteration summary and work log
- `prompts/iteration-###.txt`: exact prompt sent to the provider
- `raw/iteration-###.txt`: raw provider response

## Session Rules

A loop session is bound to:

- goal
- source path
- provider
- model

If those differ, start a separate session with `--memory-root`.

Starting a new session resets:

- `memory/`
- `logs/`
- `prompts/`
- `raw/`

## Execution Flow

1. Resolve project root and provider
2. Open or create the workspace
3. Start a new session if a goal is provided, otherwise continue the active session
4. Read memory documents
5. Build one loop prompt
6. Send it to `gh copilot`
7. Save prompt, raw output, and iteration log
8. Parse structured JSON if possible
9. Normalize plain text output if needed
10. Persist updated session state

## Provider Presets

Current presets:

- `gh-copilot-claude` -> `claude-sonnet-4`
- `gh-copilot-gpt5` -> `gpt-5`

If no provider is saved, the default is `gh-copilot-claude`.

## Under the Hood

Provider invocation:

```text
gh copilot --model <resolvedModel> [--add-dir <sourcePath>] --yolo -s -p <prompt>