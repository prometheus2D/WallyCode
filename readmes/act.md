# Act Workflow

Use `act` when WallyCode should complete an implementation-oriented request and may change files.

`act` is a shortcut for:

```powershell
wallycode loop "..." --definition act
```

The `act` definition has one step named `prompt`. Its instructions tell the provider to make the smallest correct change set and preserve existing behavior unless the request requires otherwise.

## Basic usage

```powershell
wallycode act "Add a setup tutorial README." --source C:\src\MyRepo --log --verbose
```

From the WallyCode source tree while developing WallyCode itself:

```powershell
dotnet run --project WallyCode.Console -- act "Update the development-mode documentation." --source . --memory-root .wallycode-dev --log --verbose
```

## Before running act

- Make sure the target repo is the one you expect with `--source`.
- Use `--memory-root` if another session is already active.
- Prefer `ask` first when the request needs investigation but not edits yet.
- Start from a known git state so the resulting diff is easy to review.

## After running act

Inspect the diff and run the relevant validation command. For WallyCode itself, the usual check is:

```powershell
dotnet test WallyCode.sln
```

If the active session is blocked, answer it and continue:

```powershell
wallycode respond "Use the existing command option style." --source C:\src\MyRepo
wallycode resume --step --source C:\src\MyRepo
```
