# Development Mode

Development mode means running the current WallyCode source build while you edit WallyCode itself. There is no separate `devmode` CLI verb; use `dotnet run --project WallyCode.Console -- ...` to pass commands to the local console app.

## Build and test first

From the repository root:

```powershell
dotnet restore WallyCode.sln
dotnet build WallyCode.sln
dotnet test WallyCode.sln
```

## Run the local CLI

The `--` separator passes the remaining arguments to WallyCode:

```powershell
dotnet run --project WallyCode.Console -- help
dotnet run --project WallyCode.Console -- provider --source .
```

When WallyCode should operate on this repository, use `--source .`. Use an ignored `.wallycode-dev` memory root to keep development sessions separate from normal `.wallycode` state:

```powershell
dotnet run --project WallyCode.Console -- setup --directory .
dotnet run --project WallyCode.Console -- ask "Explain the workflow command surface." --source . --memory-root .wallycode-dev --log --verbose
dotnet run --project WallyCode.Console -- act "Update the docs for the ask workflow." --source . --memory-root .wallycode-dev --log --verbose
```

The repo `.gitignore` ignores `.wallycode*`, so `.wallycode-dev` stays out of source control.

## Use the shell while editing

The shell keeps `--source`, `--memory-root`, and logging defaults attached to each subcommand:

```powershell
dotnet run --project WallyCode.Console -- shell --source . --memory-root .wallycode-dev --log --verbose
```

Inside the shell, omit the executable name:

```text
wallycode> ask "What files define the command surface?"
wallycode> act "Add a focused README section for development mode."
wallycode> reset-memory
wallycode> exit
```

## Visual Studio build mode

When the app is launched from its build output, `--vs-build` resolves the workspace root above `bin\Debug` or `bin\Release`:

```powershell
dotnet run --project WallyCode.Console -- setup --vs-build
dotnet run --project WallyCode.Console -- shell --vs-build --log --verbose
```

This is useful when debugging the console app from Visual Studio and you want WallyCode to operate on the repository root rather than the build output directory.

## Safe edit loop

1. Build and test the solution.
2. Run `ask` first if you only need analysis.
3. Run `act` when file edits are intended.
4. Inspect the resulting diff.
5. Run `dotnet test WallyCode.sln` before keeping the change.
