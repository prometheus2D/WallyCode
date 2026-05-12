# Setup and Providers

Use this tutorial to prepare one repository for WallyCode.

## Inputs

- Required: target repository path.
- Optional: provider choice and model choice.
- Optional: logging defaults for that repo.

Example values used below:
- Repo path: C:\src\MyRepo
- Provider: gh-copilot-claude
- Model: any model returned by the models command

## Step 1: Initialize repo settings

```powershell
wallycode setup --directory C:\src\MyRepo
```

Expected outcome:
- Creates C:\src\MyRepo\wallycode.json.
- Creates C:\src\MyRepo\.wallycode.

If launched from a Visual Studio build output folder, use:

```powershell
wallycode setup --vs-build
```

Use force only when you want to regenerate defaults:

```powershell
wallycode setup --directory C:\src\MyRepo --force
```

Expected outcome:
- Recreates setup artifacts with default values.

## Step 2: List providers and readiness

```powershell
wallycode provider --source C:\src\MyRepo
```

Expected outcome:
- Prints available providers.
- Shows readiness status for each provider.

Built-in providers:
- gh-copilot-claude
- gh-copilot-gpt

## Step 3: Set default provider

```powershell
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
```

Expected outcome:
- Saves default provider in wallycode.json.
- Sets a default model for that provider if needed.

## Step 4: List models and set default model

```powershell
wallycode provider gh-copilot-claude --models --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model <model-from-previous-list> --source C:\src\MyRepo
```

Expected outcome:
- First command prints available models.
- Second command saves selected model in wallycode.json.

## Step 5: Optional workspace logging defaults

Enable default logging for future commands:

```powershell
wallycode logging --enable --verbose --source C:\src\MyRepo
```

Expected outcome:
- Repo logging defaults are persisted.

Disable later if needed:

```powershell
wallycode logging --disable --quiet --source C:\src\MyRepo
```

Expected outcome:
- Logging defaults are persisted as disabled and non-verbose.

## Step 6: Verify setup

```powershell
wallycode status --source C:\src\MyRepo
```

Expected outcome:
- Prints the resolved source path and memory root.
- Shows the configured provider and model.
- Shows `Session: (none)` if no session has been started yet.
