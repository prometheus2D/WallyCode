# Development Mode

Use development mode when you are editing WallyCode itself and want to run the local source build.

There is no separate devmode verb. Use dotnet run with WallyCode arguments after --.

## Prerequisites

Run [Setup and providers](setup.md) first, or use `dotnet run --project WallyCode.Console -- setup` for local source builds. The setup command must create wallycode.json and .wallycode in your target repository.

## Inputs

- Repository root for WallyCode.
- Optional isolated memory root, typically .wallycode-dev.
- Optional source path override when running against another repo.

Example values used below:
- Source path: .
- Memory root: .wallycode-dev

Tutorial test:
- DevelopmentModeTutorialTests.Isolated_runtime_root_can_be_resolved_for_local_source_build_workflows

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

## Step 3: Run ask and act against this repo

```powershell
dotnet run --project WallyCode.Console -- ask "Explain the workflow command surface." --source . --memory-root .wallycode-dev --log --verbose
dotnet run --project WallyCode.Console -- act "Update docs for the ask workflow." --source . --memory-root .wallycode-dev --log --verbose
```

Acceptance criteria:
- Both commands exit with code 0.
- .\.wallycode-dev\session.json exists.

```powershell
Test-Path .\.wallycode-dev\session.json
```

## Step 4: Use shell for repeated commands

```powershell
dotnet run --project WallyCode.Console -- shell --source . --memory-root .wallycode-dev --log --verbose
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

