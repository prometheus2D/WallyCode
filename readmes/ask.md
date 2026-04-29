# Ask Workflow

Use `ask` when you want WallyCode to answer a question against a repo without intending to modify files.

`ask` is a shortcut for:

```powershell
wallycode loop "..." --definition ask
```

The `ask` definition has one step named `prompt`. Its instructions tell the provider not to change files, and its allowed outcomes are `[DONE]` and `[ERROR]`.

## Basic usage

```powershell
wallycode ask "What does this repository do?" --source C:\src\MyRepo --log --verbose
```

From the WallyCode source tree while developing WallyCode itself:

```powershell
dotnet run --project WallyCode.Console -- ask "Summarize the command handlers." --source . --memory-root .wallycode-dev --log --verbose
```

## When to use it

Use `ask` for:

- Repository summaries.
- Architecture questions.
- Explaining command behavior.
- Checking where a feature is implemented.
- Getting a plan before running `act`.

## Session notes

Every run is still a WallyCode session. Runtime state goes to `.wallycode` unless `--memory-root` is supplied. If you want disposable analysis sessions, use a separate memory root:

```powershell
wallycode ask "Trace the setup flow." --source C:\src\MyRepo --memory-root C:\temp\wally-ask
```

If a prior terminal session exists, WallyCode archives it when you start a new session with a new goal.
