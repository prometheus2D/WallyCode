# Setup and Remote Workspace Design

## Goal

WallyCode should be easy to move, easy to install, and easy to point at any workspace.

The target user flow is simple:

1. Copy one published WallyCode build to a machine.
2. Run one setup command.
3. Point WallyCode at any repo with `--source`.
4. Keep repo settings and session state with the repo, not with the executable.

## Terms

Use these terms consistently when we implement the setup flow:

- Install location: the folder that contains `wallycode.exe` and any bundled runtime assets.
- Source workspace: the repo or folder WallyCode operates on through `--source`.
- Runtime workspace: the `.wallycode` folder for logs, prompts, raw output, and session state, or the folder provided by `--memory-root`.
- Project settings: `wallycode.json` stored in the source workspace.

## What already works

The current code already supports most of the remote-workspace model:

- `--source` already lets the executable operate on a different repo.
- `--memory-root` already lets the session workspace live somewhere else.
- `wallycode.json` is already saved per source workspace.
- `loop`, `prompt`, `respond`, `provider`, and `shell` already resolve behavior from the source workspace, not from the executable folder.

This means the core idea already makes sense: one installed executable can operate on many repos.

## Current gap

The remaining setup gap is install packaging.

Today, runtime content is loaded from beside the executable:

- routing definitions are read from `Routing/Definitions` under the app base directory
- tutorials are intended to be read from `Tutorials` under the app base directory

Because of that, copying only the `.exe` is not enough yet unless one of these is true:

1. the published build copies the companion files with it
2. the app embeds these assets into the assembly
3. the publish output is a true single-file bundle that still gives the app access to its data

## Proposed command shape

Add a `setup` command as the main install entry point.

Suggested behavior:

```text
wallycode setup
wallycode setup --install-dir C:\Tools\WallyCode
wallycode setup --source C:\src\my-repo
wallycode setup --install-dir C:\Tools\WallyCode --source C:\src\my-repo
```

The command should do the following:

1. Resolve the current running executable or published app folder.
2. Copy WallyCode into the target install directory.
3. Ensure required runtime assets are copied with it.
4. Optionally initialize a target source workspace if `--source` is provided.
5. Print the exact next commands the user should run.

## Self-copy behavior

The setup command should be able to copy the currently running build to a new location.

That means the implementation should treat the current app location as the source of truth and copy either:

- the full published app directory in phase 1
- only the single executable in phase 2 after embedded assets or single-file publish is in place

This gives us a clean story for "I have the exe, now install it somewhere useful."

## Workspace initialization

If `--source` is provided, `setup` should treat that path as the workspace WallyCode will operate on.

Suggested first-pass behavior:

1. validate the workspace path exists
2. create `wallycode.json` if it does not exist yet
3. leave provider selection at the default unless the user passes an explicit provider or model
4. print the exact command to start with, for example:

```text
wallycode provider --source C:\src\my-repo
wallycode loop --definition ask "Summarize this repository in one short paragraph." --source C:\src\my-repo
```

## Remote usage examples

These are the scenarios the docs and command design should support clearly:

```text
wallycode prompt "Summarize this repository." --source C:\src\repo-a
wallycode loop "Build tic-tac-toe." --source D:\projects\demo-app
wallycode loop --definition act "Create a chapter outline for a novella." --source E:\writing\moon-market-book
wallycode shell --source C:\src\repo-a
```

In every case:

- the installed executable can live elsewhere
- the source workspace is the repo passed by `--source`
- the session workspace stays under that source repo by default

## Recommended implementation order

Phase 1:

1. add `setup`
2. copy the published app directory, not just the exe
3. include tutorials and routing definitions as copied content
4. keep PATH updates manual and print exact instructions instead of editing the user environment automatically

Phase 2:

1. decide whether to embed routing definitions and tutorials or move to a single-file publish model
2. once runtime assets no longer depend on sibling files, allow true exe-only self-copy
3. optionally add a stronger install experience such as a stable default install directory

## Open decisions

These still need a product call before code changes:

1. Default install directory: should we use a user-local folder like `%LocalAppData%\WallyCode\bin` or a simple path under the current working folder?
2. PATH handling: should `setup` only print instructions, or should it offer an explicit flag to update PATH?
3. Workspace init: should `setup --source` create `wallycode.json` only, or also let the user pick provider and model during setup?
4. Packaging: do we want to keep external content files for easy editing, or embed them so a lone exe is enough?

## Short conclusion

Yes, the idea makes sense.

WallyCode already has the right workspace model for remote use through `--source`. The missing work is mostly around packaging and a clearer install command, not around the core execution model.