# Setup and Providers

Use this tutorial to initialize WallyCode state and optionally install a repo-local WallyCode executable payload.

## Invocation

Run commands from the folder containing the source `wallycode.exe`, unless a step switches into an installed repo-local copy. Replace `C:\src\MyRepo` with the target source root.

Defaults used below:
- Repo path: C:\src\MyRepo
- Provider: gh-copilot-claude
- Model: claude-haiku-4.5

## Step 1: Full setup and install

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --install
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo exists.
- C:\src\MyRepo\wallycode.json exists.
- C:\src\MyRepo\.wallycode exists.
- C:\src\MyRepo\wallycode.exe exists.
- C:\src\MyRepo\wallycode.active.json points to C:\src\MyRepo.
- C:\src\MyRepo\wallycode.install.json exists.
- wallycode.active.json next to the source exe also points to C:\src\MyRepo.

```powershell
Test-Path C:\src\MyRepo
Test-Path C:\src\MyRepo\wallycode.json
Test-Path C:\src\MyRepo\.wallycode
Test-Path C:\src\MyRepo\wallycode.exe
Test-Path C:\src\MyRepo\wallycode.install.json
((Get-Content .\wallycode.active.json -Raw | ConvertFrom-Json).activeProjectPath -eq 'C:\src\MyRepo')
```

Dev-build shortcut from a supported build output folder:

```powershell
.\wallycode.exe setup --vs-build --install
```

Expected behavior for --vs-build:
- WallyCode resolves the workspace root above that output folder.
- setup artifacts and the installed payload are written to the resolved workspace root.
- wallycode.active.json next to the build-output exe points to the resolved workspace root.

## Step 1b: Setup only

Use setup without install when the workspace should use a central WallyCode executable instead of a repo-local copy.

```powershell
.\wallycode.exe setup --source C:\src\MyRepo
```

Expected outcome:
- Creates or updates wallycode.json and .wallycode.
- Updates the active pointer next to the executable you are running.
- Does not copy wallycode.exe or Loadables into the source root.

## Step 1c: Install only

Install is separate from setup by default. It removes any previous local WallyCode payload in the target folder, copies the current executable payload, and writes a source-local active pointer. It does not create or reset workspace setup state unless you add `--setup`.

```powershell
.\wallycode.exe install --source C:\src\MyRepo
```

Expected outcome:
- Exit code is 0.
- C:\src\MyRepo\wallycode.exe exists.
- C:\src\MyRepo\wallycode.active.json points to C:\src\MyRepo.
- C:\src\MyRepo\wallycode.install.json exists.
- Any stale installed `Loadables` from a previous local install are gone.

```powershell
Test-Path C:\src\MyRepo\wallycode.exe
Test-Path C:\src\MyRepo\wallycode.install.json
((Get-Content C:\src\MyRepo\wallycode.active.json -Raw | ConvertFrom-Json).activeProjectPath -eq 'C:\src\MyRepo')
```

Use the repo-local executable:

```powershell
Set-Location C:\src\MyRepo
.\wallycode.exe status
```

To install and setup the workspace in one pass:

```powershell
.\wallycode.exe install --source C:\src\MyRepo --setup
```

Expected outcome:
- The local executable payload is freshly installed.
- wallycode.json and .wallycode are recreated with setup defaults.
- Any old workspace session state is removed.

## Step 1d: Optional cleanup + regenerate

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --cleanup
```

Expected outcome:
- Removes existing wallycode.json and .wallycode first.
- Recreates setup artifacts with default values.
- Updates wallycode.active.json to point at C:\src\MyRepo.
- Does not install or uninstall a local executable payload.

## Step 1e: Optional uninstall local executable

```powershell
.\wallycode.exe uninstall --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\wallycode.exe does not exist.
- C:\src\MyRepo\Loadables does not exist.
- C:\src\MyRepo\wallycode.active.json does not exist.
- C:\src\MyRepo\wallycode.install.json does not exist.
- Existing workspace setup state is not removed by uninstall when uninstall is run from a different exe folder.
- If uninstall is run from C:\src\MyRepo with the installed executable, WallyCode also removes workspace setup state and warns that the running application cannot remove itself immediately.

```powershell
Test-Path C:\src\MyRepo\wallycode.exe
Test-Path C:\src\MyRepo\Loadables
Test-Path C:\src\MyRepo\wallycode.active.json
Test-Path C:\src\MyRepo\wallycode.install.json
Test-Path C:\src\MyRepo\wallycode.json
```

Expected output is `False`, `False`, `False`, `False`, then `True` if setup state still exists.

## Step 1f: Remove workspace setup artifacts cleanly

```powershell
.\wallycode.exe cleanup --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- C:\src\MyRepo\wallycode.json does not exist.
- C:\src\MyRepo\.wallycode does not exist.
- If C:\src\MyRepo was active, wallycode.active.json is removed.
- If you skipped uninstall, the installed local executable payload remains in place.

```powershell
Test-Path C:\src\MyRepo\wallycode.json
Test-Path C:\src\MyRepo\.wallycode
```

Expected output for both checks is `False`.

Recreate setup state after cleanup:

```powershell
.\wallycode.exe setup --source C:\src\MyRepo
```

## Step 2: List providers and readiness

```powershell
.\wallycode.exe provider
```

Acceptance criteria:
- Exit code is 0.
- Output includes provider names and readiness status lines.

Built-in providers:
- gh-copilot-claude
- gh-copilot-gpt

## Step 3: Set default provider

```powershell
.\wallycode.exe provider gh-copilot-claude --set --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- wallycode.json property provider equals gh-copilot-claude.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).provider -eq 'gh-copilot-claude')
```

## Step 4: List models and set default model

```powershell
.\wallycode.exe provider gh-copilot-claude --models --source C:\src\MyRepo
.\wallycode.exe provider gh-copilot-claude --model claude-haiku-4.5 --source C:\src\MyRepo
```

Acceptance criteria:
- Both commands exit with code 0.
- wallycode.json property model equals the selected model.

```powershell
((Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json).model -eq 'claude-haiku-4.5')
```

## Step 5: Optional workspace logging defaults

Enable default logging for future commands:

```powershell
.\wallycode.exe logging --enable --verbose --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- wallycode.json logging.enabled is True.
- wallycode.json logging.verbose is True.

```powershell
$settings = Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json
($settings.logging.enabled -eq $true)
($settings.logging.verbose -eq $true)
```

Disable later if needed:

```powershell
.\wallycode.exe logging --disable --quiet --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- wallycode.json logging.enabled is False.
- wallycode.json logging.verbose is False.

```powershell
$settings = Get-Content C:\src\MyRepo\wallycode.json -Raw | ConvertFrom-Json
($settings.logging.enabled -eq $false)
($settings.logging.verbose -eq $false)
```

## Step 6: Verify setup

```powershell
.\wallycode.exe status --source C:\src\MyRepo
```

Acceptance criteria:
- Exit code is 0.
- Output contains Source:, Provider:, and Model:.
- Output contains Session: (none) when no session exists.

Follow-up behavior:
- run/ask/act/step require setup artifacts in the target workspace.
- provider/logging/status/shell also require initialized setup state.
- respond/resume/recover require an existing workspace session.

## Step 7: Use workflows immediately

After setup writes the active source pointer, these commands can run from the exe folder without repeating `--source`. If you used install, run these from C:\src\MyRepo with the installed `wallycode.exe`.

Use `ask` for a one-shot question:

```powershell
.\wallycode.exe ask "What does this project do?"
```

Use `act` for a one-shot implementation action:

```powershell
.\wallycode.exe act "Add a short README section describing how to run the project."
```

Use `run` for the workflow loop. The default `requirements` workflow collects requirements, produces tasks, and executes tasks:

```powershell
.\wallycode.exe run "Build a simple browser Tic Tac Toe game with a README." requirements --log --verbose
```

If the session blocks:

```powershell
.\wallycode.exe respond "Proceed with the simplest browser-only implementation."
```

If the session is still active:

```powershell
.\wallycode.exe resume
```

If requirements are already clear and you want to start at task creation:

```powershell
.\wallycode.exe run "Create tasks for adding score tracking, then implement them." tasks --log --verbose
```

Acceptance criteria:
- Each command exits with code 0.
- Workflow commands create or update a session under C:\src\MyRepo\.wallycode.
- If a workflow blocks, `respond` exits with code 0 and resumes the session.
