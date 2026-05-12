# Ask Workflow

Use ask for analysis-only goals.

ask is equivalent to running run with workflow ask.

## Inputs

- Required to start a new session: prompt text.
- Optional: source path.
- Optional: memory-root for isolated session state.
- Optional: provider or model override.
- Optional: max-run-iterations, max-total-iterations, max-step-repeats.

## Step 1: Ask a question

```powershell
wallycode ask "What does this repository do?" --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Starts or continues an ask workflow session.
- Writes session/runtime state under .wallycode unless memory-root is set.

## Step 2: Continue if still active

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Continues the same active session.

## Step 3: Respond if blocked

```powershell
wallycode respond "Focus on command handlers and workflow transitions." --source C:\src\MyRepo --log --verbose
```

Expected outcome:
- Saves response text and resumes automatically.

## Optional: isolate analysis sessions

```powershell
wallycode ask "Trace the setup flow." --source C:\src\MyRepo --memory-root C:\temp\wally-ask --log --verbose
```

Expected outcome:
- Uses a separate runtime root so it does not affect the default .wallycode session.

## Optional: local source-build usage

```powershell
dotnet run --project WallyCode.Console -- ask "Summarize the command handlers." --source . --memory-root .wallycode-dev --log --verbose
```

Expected outcome:
- Executes ask using the current local source build.
