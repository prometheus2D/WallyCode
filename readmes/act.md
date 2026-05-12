# Act Workflow

Use act for implementation goals where file changes are expected.

act is equivalent to running run with workflow act.

## Inputs

- Required to start a new session: prompt text with desired change.
- Optional: source path.
- Optional: memory-root for isolated session state.
- Optional: provider or model override.
- Optional: max-run-iterations, max-total-iterations, max-step-repeats.

Example values used below:
- Repo path: C:\src\MyRepo
- Isolated memory root: C:\temp\wally-act

## Step 1: Start an implementation session

```powershell
wallycode act "Add a setup tutorial README." --source C:\src\MyRepo --log --verbose
```

Required assertions:
- Exit code is 0.
- C:\src\MyRepo\.wallycode\session.json exists.

```powershell
Test-Path C:\src\MyRepo\.wallycode\session.json
```

## Step 2: Continue until completion or block

```powershell
wallycode resume --source C:\src\MyRepo --log --verbose
```

Required assertions:
- Exit code is 0.
- Session snapshot folder exists after one or more iterations.

```powershell
Test-Path C:\src\MyRepo\.wallycode\sessions
```

## Step 3: Respond if blocked

```powershell
wallycode respond "Use the existing command option style." --source C:\src\MyRepo --log --verbose
```

Required assertions:
- If blocked, exit code is 0 and run continues.
- If not blocked, command explains no blocked session is waiting.

## Optional: allow more work per invocation

```powershell
wallycode act "Fix these code problems: <paste problems here>" --source C:\src\MyRepo --max-run-iterations 40 --log --verbose
```

Required assertions:
- Exit code is 0.
- session.json iterationCount increases compared to before the command.

## Optional: local source-build usage

```powershell
dotnet run --project WallyCode.Console -- act "Update development-mode documentation." --source . --memory-root .wallycode-dev --log --verbose
```

Required assertions:
- Exit code is 0.
- .wallycode-dev\session.json exists in the current repository.

```powershell
Test-Path .\.wallycode-dev\session.json
```
