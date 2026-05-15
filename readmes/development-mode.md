# Development Mode

Development mode means running a WallyCode executable from a build-output folder while targeting the source root. This guide assumes the executable already exists; it only covers WallyCode commands.

## Step 1: Resolve source root and install locally

From the folder containing the dev-built `wallycode.exe`:

```powershell
.\wallycode.exe setup --vs-build --install
```

Acceptance criteria:
- Exit code is 0.
- WallyCode resolves the source root above the build-output folder.
- The resolved source root contains wallycode.json, .wallycode, wallycode.exe, wallycode.active.json, and wallycode.install.json.
- The build-output active pointer targets the resolved source root.

## Step 2: Use the repo-local executable

```powershell
Set-Location <resolved-source-root>
.\wallycode.exe status
.\wallycode.exe ask "Explain the workflow command surface." --log --verbose
.\wallycode.exe act "Update docs for the ask workflow." --log --verbose
```

Acceptance criteria:
- status prints Source:, Provider:, Model:, and Session:.
- Workflow commands use .\.wallycode\session.json under the source root.

## Step 3: Reinstall after changing WallyCode

From the build-output folder again:

```powershell
.\wallycode.exe setup --vs-build --install
```

Expected behavior:
- Old workspace state is removed and recreated.
- Old repo-local WallyCode payload is removed before the new payload is copied.
- Stale installed Loadables are gone.

## Step 4: Repeated commands

```powershell
.\wallycode.exe shell --source <resolved-source-root> --log --verbose
```

Example shell usage:

```text
wallycode> ask "What files define the command surface?"
wallycode> act "Add a focused README section for development mode."
wallycode> reset-memory
wallycode> exit
```
