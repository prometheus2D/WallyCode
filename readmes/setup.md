# Setup and Providers

Use this guide when preparing a repo that WallyCode should operate on.

## Initialize a repo

```powershell
wallycode setup --directory C:\src\MyRepo
```

Setup creates:

- `wallycode.json` for repo-scoped settings.
- `.wallycode` for active session state and archives.

If WallyCode is launched from a Visual Studio build output, `--vs-build` resolves setup back to the workspace root above `bin\Debug` or `bin\Release`:

```powershell
wallycode setup --vs-build
```

Use `--force` only when you want to recreate `wallycode.json` and `.wallycode` with defaults:

```powershell
wallycode setup --directory C:\src\MyRepo --force
```

## Check providers

```powershell
wallycode provider --source C:\src\MyRepo
```

The provider list includes readiness. A provider can be unavailable if GitHub CLI is missing, Copilot CLI is missing, or `gh auth status` is not authenticated.

Built-in providers:

- `gh-copilot-claude`
- `gh-copilot-gpt5`

## Set defaults

```powershell
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
wallycode provider gh-copilot-claude --models --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model claude-sonnet-4 --source C:\src\MyRepo
```

Provider and model defaults are saved in `wallycode.json` for that repo. A single invocation can still override them with `--provider` and `--model`.

## Workspace logging defaults

For repeated development runs, saving logging defaults can be nicer than typing `--log --verbose` every time:

```powershell
wallycode logging --enable --verbose --source C:\src\MyRepo
```

Disable them later with:

```powershell
wallycode logging --disable --quiet --source C:\src\MyRepo
```
