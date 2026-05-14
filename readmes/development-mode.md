# Development Mode

Use development mode when you are editing WallyCode itself and want to run the local source build.

There is no separate devmode verb. Use dotnet run with WallyCode arguments after --.

## Prerequisites

Required: initialize workspace state before workflow commands.

```powershell
dotnet run --project WallyCode.Console -- setup --source .
```

## Inputs

- Repository root for WallyCode.
- Optional source path override when running against another repo.

Example values used below:
- Source path: .

Manual test:
- Use the local source-build commands and acceptance criteria below.

## Step 1: Run local CLI help

```powershell
dotnet run --project WallyCode.Console -- help
```

Acceptance criteria:
- Exit code is 0.
- Output includes the command surface help text.

## Step 2: Initialize local repo state

```powershell
dotnet run --project WallyCode.Console -- setup --source .
```

Acceptance criteria:
- Exit code is 0.
- .\wallycode.json exists.
- .\.wallycode exists.

```powershell
Test-Path .\wallycode.json
Test-Path .\.wallycode
```

Optional clean reset:

```powershell
dotnet run --project WallyCode.Console -- cleanup --source .
dotnet run --project WallyCode.Console -- setup --source .
```

## Step 3: Run ask and act against this repo

```powershell
dotnet run --project WallyCode.Console -- ask "Explain the workflow command surface." --source . --log --verbose
dotnet run --project WallyCode.Console -- act "Update docs for the ask workflow." --source . --log --verbose
```

Acceptance criteria:
- Both commands exit with code 0.
- .\.wallycode\session.json exists.

```powershell
Test-Path .\.wallycode\session.json
```

## Step 4: Use shell for repeated commands

```powershell
dotnet run --project WallyCode.Console -- shell --source . --log --verbose
```

Acceptance criteria:
- Exit code is 0 when shell exits normally.
- Interactive prompt appears.

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

Acceptance criteria:
- setup --vs-build exits with code 0 when launched from a supported build-output context.
- shell --vs-build resolves to workspace root and starts normally.
- setup artifacts are read from the resolved workspace root, not from the build output folder.
