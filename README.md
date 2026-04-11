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

Run a prompt in the current repo:

```text
prompt "Summarize this repository in one short paragraph."
```

Run a prompt against a different folder:

```text
prompt "Summarize this repository in one short paragraph." --source C:\src\my-repo
```

That is the fastest way to get started.

## Simple Loop Start

Use `loop` when you want WallyCode to keep state between iterations.

Start a loop:

```text
loop "Analyze this repo, do one bounded chunk of work, update memory, and stop when the goal is complete."
```

Continue the active loop:

```text
loop
```

Add user input for the next iteration:

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
4. If you want iterative progress, switch to `loop`.

One-shot prompt example:

```text
prompt "Create a simple browser-based tic-tac-toe game in this repo. Keep it minimal: HTML, CSS, and JavaScript only. Add clear file-by-file steps and then implement the code."
```

Iterative loop example:

```text
loop "Build a simple browser-based tic-tac-toe game in this repo. Do one small bounded chunk per iteration, keep the implementation minimal, and stop when the game is complete."
```

Then continue as needed:

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

You can also override provider or model for a single prompt or new loop session:

```text
prompt "Summarize this repository" --provider gh-copilot-gpt5
prompt "Summarize this repository" --model gpt-5
```

## Files Written

WallyCode stores project settings in `wallycode.json`.

`source` is the folder WallyCode and the provider operate against.

`memory-root` is the folder where WallyCode stores loop memory, prompts, raw output, logs, and session state.

Runtime files are written under `.wallycode/` by default, or under the folder passed to `--memory-root`.