# WallyCode

Deterministic CLI workflows for getting real progress on a codebase with durable session state.

## Fast Path

Open a terminal beside `wallycode.exe`. WallyCode is source-root oriented: `setup` creates workspace state, `install` copies the runnable WallyCode payload into a target, and workflow commands operate on the active source recorded in `wallycode.active.json`.

Fresh workspace plus local executable:

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --install
Set-Location C:\src\MyRepo
.\wallycode.exe status
.\wallycode.exe provider gh-copilot-claude --set
.\wallycode.exe provider gh-copilot-claude --model claude-haiku-4.5
.\wallycode.exe run "Collect requirements, produce tasks, then implement the requested change." requirements --log --verbose
```

Dev-build flow from a supported build output folder:

```powershell
.\wallycode.exe setup --vs-build --install
Set-Location <resolved-source-root>
.\wallycode.exe status
```

Refresh an installed WallyCode payload and reset workspace state:

```powershell
.\wallycode.exe install --source C:\src\MyRepo --setup
```

Remove the installed payload:

```powershell
.\wallycode.exe uninstall --source C:\src\MyRepo
```

## Command Model

| Command | State touched | Use when |
| --- | --- | --- |
| `setup --source <repo>` | Creates/updates `<repo>\wallycode.json`, `<repo>\.wallycode`, and the exe-local active pointer. | You want workspace state only. |
| `setup --source <repo> --cleanup` | Removes old workspace state first, then recreates it. | You want a clean WallyCode session/runtime state without touching the installed exe. |
| `setup --source <repo> --install` | Cleans workspace state, recreates it, removes old local payload, then installs the current WallyCode payload into `<repo>`. | Fast full source-workspace setup. Best default for a repo-local WallyCode executable. |
| `install --source <repo>` | Removes prior local payload, copies `wallycode.exe`, runtime files, `Loadables`, writes `wallycode.active.json` and `wallycode.install.json`. | You want only the local executable payload refreshed. |
| `install --source <repo> --setup` | Installs payload, then resets workspace state. | You are starting from install but also want fresh setup. |
| `cleanup --source <repo>` | Removes `wallycode.json` and `.wallycode`; clears the active pointer for the running exe if it points there. | You want workspace state removed, not the installed payload. |
| `uninstall --source <repo>` | Removes installed payload, copied `Loadables`, source-local active pointer, and install manifest. | You want WallyCode executable files gone from that repo. |

If `uninstall` targets the directory of the running executable, WallyCode also removes workspace state there and warns that locked app files cannot be removed until process exit. On Windows, locked files are scheduled for deferred removal.

`setup --vs-build` resolves the source root from a supported build-output launch path. Combine it with `--install` to quickly install the current WallyCode payload into the resolved source root.

## Workflow Loop

`run` starts or continues a workflow. The default `requirements` workflow collects requirements, produces tasks, then executes tasks.

```powershell
.\wallycode.exe run "Add score tracking and update the README." requirements --log --verbose
.\wallycode.exe respond "Prefer the smallest UI change that preserves current behavior."
.\wallycode.exe resume
```

Use `ask` for one-shot analysis and `act` for one-shot implementation:

```powershell
.\wallycode.exe ask "Summarize the command surface."
.\wallycode.exe act "Add a setup note for installed WallyCode payloads."
```

Commands requiring initialized setup: `run`, `ask`, `act`, `step`, `provider`, `logging`, `status`, `shell`, `recover`.

Commands requiring an existing session: `resume`, `respond`.

Common workflow flags: `--source <path>`, `--log`, `--verbose`, `--max-run-iterations <n>`, `--max-total-iterations <n>`, `--max-step-repeats <n>`.

Project state lives in `wallycode.json` and `.wallycode`. The active source pointer lives in `wallycode.active.json` next to whichever `wallycode.exe` you are running.

## Smoke Path

```powershell
.\wallycode.exe setup --source C:\src\MyRepo --install
Set-Location C:\src\MyRepo
.\wallycode.exe status
.\wallycode.exe provider
.\wallycode.exe ask "Summarize this repository in one paragraph." --log --verbose
.\wallycode.exe run "Collect requirements for a small README update." requirements --max-run-iterations 1 --log --verbose
```

Expected artifacts: `wallycode.json`, `.wallycode`, `.wallycode\session.json`, `wallycode.exe`, `wallycode.active.json`, and `wallycode.install.json` in the source root after the commands above.

## Use tutorials for explicit, testable steps

- [Tutorials index](tutorials/README.md)
- [Setup and providers](tutorials/setup.md)
- [Scratch project from a new folder](tutorials/scratch-project.md)
- [Ask workflow](tutorials/ask.md)
- [Act workflow](tutorials/act.md)
- [Stepwise workflows](tutorials/stepwise.md)
- [Definitions and steps](tutorials/definitions.md)
- [Development mode](tutorials/development-mode.md)

## Core commands

```powershell
.\wallycode.exe setup
.\wallycode.exe install
.\wallycode.exe uninstall
.\wallycode.exe cleanup
.\wallycode.exe provider
.\wallycode.exe status
.\wallycode.exe run
.\wallycode.exe ask
.\wallycode.exe act
.\wallycode.exe resume
.\wallycode.exe respond
.\wallycode.exe recover
.\wallycode.exe step
.\wallycode.exe shell
```
