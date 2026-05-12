# Development Mode

Use development mode when you are editing WallyCode itself and want to run the local source build.

There is no separate devmode verb. Use dotnet run with WallyCode arguments after --.

## Inputs

- Repository root for WallyCode.
- Optional isolated memory root, typically .wallycode-dev.
- Optional source path override when running against another repo.

## Step 1: Run local CLI help

```powershell
dotnet run --project WallyCode.Console -- help
```

Expected outcome:
- Runs the local WallyCode build and prints command help.

## Step 2: Initialize local repo state

```powershell
dotnet run --project WallyCode.Console -- setup --directory .
```

Expected outcome:
- Creates or updates local repo setup files for this workspace.

## Step 3: Run ask and act against this repo

```powershell
dotnet run --project WallyCode.Console -- ask "Explain the workflow command surface." --source . --memory-root .wallycode-dev --log --verbose
dotnet run --project WallyCode.Console -- act "Update docs for the ask workflow." --source . --memory-root .wallycode-dev --log --verbose
```

Expected outcome:
- Uses local source build.
- Keeps development session data in .wallycode-dev instead of .wallycode.

## Step 4: Use shell for repeated commands

```powershell
dotnet run --project WallyCode.Console -- shell --source . --memory-root .wallycode-dev --log --verbose
```

Expected outcome:
- Starts interactive shell with shared defaults.

Example shell usage:

```text
wallycode> ask "What files define the command surface?"
wallycode> act "Add a focused README section for development mode."
wallycode> reset-memory
wallycode> exit
```

## Step 5: Optional Visual Studio build output resolution

```powershell
dotnet run --project WallyCode.Console -- setup --vs-build
dotnet run --project WallyCode.Console -- shell --vs-build --log --verbose
```

Expected outcome:
- Resolves workspace root from a standard bin\Debug or bin\Release output path.
