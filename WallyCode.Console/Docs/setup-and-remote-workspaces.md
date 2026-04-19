# Setup and Remote Workspace Design

## Goal

WallyCode should be easy to move, easy to install, and easy to point at any workspace.

The target user flow is simple:

1. Run `setup` where the app already lives to create a local Wally workspace.
2. If needed, copy the current app to another folder and set it up there.
3. Point WallyCode at any repo with `--source` when you want a different workspace.
4. Keep repo settings and session state with the chosen workspace, not with the executable process itself.

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
wallycode setup --init
wallycode setup --init --directory C:\Tools\WallyCode
wallycode setup --init --force
wallycode setup --init --install-dir C:\Tools\WallyCode
wallycode setup --init --source C:\src\my-repo
wallycode setup --init --vs-build
wallycode setup --init --install-dir C:\Tools\WallyCode --source C:\src\my-repo
```

Running `wallycode setup` by itself should not silently create or copy anything. It should print setup instructions or the help text so the user can choose the exact setup mode.

`--init` should be the flag that actually initializes a target directory.

- `wallycode setup --init` initializes the folder where the current executable is running.
- `wallycode setup --init --directory C:\somewhere` initializes the provided directory instead.
- if the provided directory is meant to become a copied install, setup should copy the current app there first and then initialize that same directory.
- `--force`, `--vs-build`, `--source`, and `--install-dir` are modifiers on `--init`, not stand-alone setup actions.

The command should do the following:

1. Resolve the current running executable or published app folder.
2. If `setup` is run with no mode flags, print help or setup instructions and stop.
3. If `--init` is provided with no directory argument, use the current app folder as the setup location.
4. If `--init` is provided with a directory argument, use that provided directory as the setup location.
5. If `wallycode.json` already exists in the chosen setup location, leave it alone.
6. If `wallycode.json` does not exist in the chosen setup location, create it.
7. If `--force` is provided with `--init`, always create a fresh setup file.
8. If `--install-dir` is provided for a copy-based setup, copy WallyCode into that target directory first.
9. Ensure required runtime assets are copied with it.
10. Initialize the setup file in the final target location.
11. If `--source` is provided, use that as the workspace WallyCode will operate on.
12. Print the exact next commands the user should run.

This keeps setup simple by default:

- `setup` by itself prints instructions or help
- `setup --init` initializes the folder where the app already runs
- `setup --init --directory <path>` initializes that provided directory instead
- remote setup copies the app to another folder and creates setup there
- `setup --init --force` recreates setup when the user wants a clean start

## Self-copy behavior

The setup command should be able to copy the currently running build to a new location.

That means the implementation should treat the current app location as the source of truth and copy either:

- the full published app directory in phase 1
- only the single executable in phase 2 after embedded assets or single-file publish is in place

This gives us a clean story for "I have the exe, now install it somewhere useful."

For the first pass, this should work as a directory copy, not as an exe-only copy. The copied location should become a valid Wally install with the setup file created there if it does not already exist.

## Workspace initialization

If `--source` is provided, `setup` should treat that path as the workspace WallyCode will operate on.

Suggested first-pass behavior:

1. if `--source` is provided, validate that path and use it as the workspace
2. otherwise, if `--init` has no directory argument, use the current app folder as the workspace
3. otherwise, if `--init` has a directory argument, use that provided directory as the workspace
4. if `--install-dir` is provided, use the copied install folder as the workspace unless `--source` overrides it
5. create `wallycode.json` if it does not exist yet
6. if `wallycode.json` already exists, leave it alone unless `--force` is provided
7. use the default provider and the default model during workspace initialization unless the user passes an explicit override
8. print the exact command to start with, for example:

```text
wallycode provider --source C:\src\my-repo
wallycode loop --definition ask "Summarize this repository in one short paragraph." --source C:\src\my-repo
```

## Visual Studio build mode

We also want a simple path for local development builds.

When WallyCode is running from a normal Visual Studio output path such as:

```text
<project>\bin\Debug\net8.0\
<project>\bin\Release\net8.0\
```

`setup --init --vs-build` should treat the parent development workspace as the setup target instead of the build output folder.

Expected behavior:

1. detect that the app is running from a standard Visual Studio build or release folder
2. walk upward to the git root for the repo the build came from
3. use that topmost git-level workspace as the setup target so test runs and local debug runs work against the same solution they were built from
4. create `wallycode.json` there if it does not exist yet
5. leave the existing file alone unless `--force` is provided

This gives the right local-development behavior: the executable can run from `bin\Debug` or `bin\Release`, but setup lands at the repo git root instead of inside the build output.

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
2. make `setup` with no arguments print help or setup instructions
3. add `--force` to recreate the setup file when needed
4. add `--init` so setup only runs when the user explicitly asks for initialization
5. add `--vs-build` for standard Visual Studio output folders and resolve to the repo git root
6. copy the published app directory, not just the exe
7. include tutorials and routing definitions as copied content
8. keep PATH updates manual and print exact instructions instead of editing the user environment automatically

Phase 2:

1. decide whether to embed routing definitions and tutorials or move to a single-file publish model
2. once runtime assets no longer depend on sibling files, allow true exe-only self-copy
3. revisit whether a stronger install experience is needed later

## Product decisions

These decisions are now settled for the first implementation:

1. There is no default install directory for now.
2. `setup` by itself should only print instructions or help.
3. Workspace initialization should use a default model.
4. Visual Studio build mode should resolve to the topmost git-level workspace.
5. Runtime content should stay as external files for easy editing.

## Short conclusion

Yes, the idea makes sense.

WallyCode already has the right workspace model for remote use through `--source`. The remaining work is to make `setup` clearer and safer: help by default, local setup when chosen, remote copy when needed, `--force` for a clean reset, git-root Visual Studio build behavior, and external runtime content that stays easy to edit.