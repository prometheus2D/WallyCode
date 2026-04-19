# Setup and Remote Workspace Design

## Goal

WallyCode should be easy to move, easy to install, and easy to point at any workspace.

The target user flow is simple:

1. Place the WallyCode app where you want it.
2. Run `setup` there, or use `setup --directory <path>` to prepare another directory.
3. Run normal WallyCode commands from the workspace you want WallyCode to operate on.
4. Keep repo settings and session state with the working directory, not with the executable process itself.

## Terms

Use these terms consistently when we implement the setup flow:

- Install location: the folder that contains `wallycode.exe` and any bundled runtime assets.
- Setup target: the folder `setup` checks and initializes.
- Working directory: the current directory normal WallyCode commands operate on.
- Runtime workspace: the `.wallycode` folder under the working directory, or the folder provided by `--memory-root`.
- Project settings: `wallycode.json` stored in the working directory.

## Runtime model

The future-state model separates setup from normal command execution.

- `setup` prepares one target directory.
- normal commands operate on the current working directory.
- `--memory-root` can place runtime state elsewhere when needed.
- the installed app can live in one location while commands run in many different repos.

This keeps setup concerns separate from day-to-day command execution.

## Runtime asset model

Runtime content lives beside the executable:

- routing definitions are read from `Routing/Definitions` under the app base directory
- tutorials are read from `Tutorials` under the app base directory

Because of that, a working deployment needs the companion files unless one of these is true:

1. the published build copies the companion files with it
2. the app embeds these assets into the assembly
3. the publish output is a true single-file bundle that still gives the app access to its data

## Proposed command shape

Add a `setup` command as the main install entry point.

Suggested behavior:

```text
wallycode setup
wallycode setup --directory C:\src\my-repo
wallycode setup --force
wallycode setup --directory C:\src\my-repo --force
wallycode setup --vs-build
```

`setup` should perform the check-and-setup work directly.

The basic rule is simple: resolve one target directory, check whether WallyCode is already set up there, create setup if it is missing, and only do a full reset when `--force` is provided.

- `wallycode setup` checks the current app folder.
- `wallycode setup --directory C:\somewhere` checks the provided directory instead.
- if setup is missing, WallyCode creates it.
- if setup already exists, WallyCode leaves it alone unless `--force` is provided.

### What each setup flag is for

- `--directory`: target directory for setup. Use this when you want setup to run somewhere other than the local app folder.
- `--vs-build`: when running from `bin\Debug` or `bin\Release`, resolve the setup target to the repo git root instead of the build output folder.
- `--force`: do a fresh setup in the target directory instead of leaving the existing setup in place.

For the basic setup case, `setup` only needs one target directory.

## How runtime targeting works

Normal WallyCode commands use the current working directory.

That means the workspace is chosen by where the user runs the command, not by a runtime path override flag in this design.

Use it like this:

```text
cd C:\src\repo-a
wallycode provider
wallycode prompt "Summarize this repository."

cd D:\projects\demo-app
wallycode loop "Build tic-tac-toe."

cd C:\src\repo-a
wallycode shell
```

The intended flow is:

1. run `wallycode setup --directory C:\src\repo-a`
2. change into `C:\src\repo-a`
3. run normal commands there

Examples:

- `wallycode setup`:
checks the current app folder. If setup is missing, create it. If setup already exists, leave it alone.
- `wallycode setup --directory C:\work\wally-home`:
checks `C:\work\wally-home`. If setup is missing, create it there.
- `wallycode setup --directory C:\work\wally-home --force`:
does a fresh setup in `C:\work\wally-home` even if WallyCode is already set up there.
- `wallycode setup --vs-build`:
when running from a Visual Studio build output, checks the repo git root and sets it up there if needed.

The command should do the following:

1. Resolve the current running executable or published app folder.
2. Resolve the setup target in this order: `--directory`, `--vs-build`, then the current app folder.
3. Check whether WallyCode is already set up in that target directory.
4. If setup is missing, create `wallycode.json` and the default runtime workspace in that directory.
5. If setup exists and `--force` is not provided, leave it alone and report that setup is already in place.
6. If setup exists and `--force` is provided, recreate the setup completely in that target directory.
7. Write the default provider and default model into the setup file.
8. Print the exact next commands the user should run.

This keeps setup simple by default:

- `setup` checks the local app folder and sets it up if needed
- `setup --directory <path>` does the same thing for a provided directory
- `setup --force` does a fresh setup in the target directory
- `setup --vs-build` targets the repo git root from a standard Visual Studio build output

## Manual placement behavior

WallyCode can still be moved by placing the published app folder where you want it and then running setup there.

`setup` does not copy the app. It verifies and initializes the chosen target directory once the app is already in place.

## Workspace initialization

`setup` works against one target directory.

Suggested first-pass behavior:

1. if `--directory` is provided, validate that path and use it as the target directory
2. otherwise, if `--vs-build` is provided, use the repo git root as the target directory
3. otherwise, use the current app folder as the target directory
4. if `wallycode.json` and the runtime workspace already exist and `--force` is not provided, treat setup as already complete
5. if setup is missing, create `wallycode.json` and the default runtime workspace in that directory
6. if `--force` is provided, remove and recreate the setup artifacts in that directory
7. write the default provider and default model during setup
8. after setup, run normal commands from the target directory
9. print the exact command to start with, for example:

```text
cd C:\src\my-repo
wallycode provider
wallycode loop --definition ask "Summarize this repository in one short paragraph."
```

## Visual Studio build mode

We also want a simple path for local development builds.

When WallyCode is running from a normal Visual Studio output path such as:

```text
<project>\bin\Debug\net8.0\
<project>\bin\Release\net8.0\
```

`setup --vs-build` should treat the parent development workspace as the setup target instead of the build output folder.

Expected behavior:

1. detect that the app is running from a standard Visual Studio build or release folder
2. walk upward to the git root for the repo the build came from
3. use that topmost git-level workspace as the setup target so test runs and local debug runs work against the same solution they were built from
4. if setup is missing there, create it
5. if setup already exists there and `--force` is not provided, leave it alone
6. if `--force` is provided, recreate the setup there completely

This gives the right local-development behavior: the executable can run from `bin\Debug` or `bin\Release`, but setup lands at the repo git root instead of inside the build output.

## Remote usage examples

These are the scenarios the docs and command design should support clearly:

```text
cd C:\src\repo-a
wallycode prompt "Summarize this repository."

cd D:\projects\demo-app
wallycode loop "Build tic-tac-toe."

cd E:\writing\moon-market-book
wallycode loop --definition act "Create a chapter outline for a novella."

cd C:\src\repo-a
wallycode shell
```

In every case:

- the installed executable can live elsewhere
- the working directory is the repo WallyCode operates on
- the session workspace stays under that working directory by default

## Recommended implementation order

Phase 1:

1. add `setup`
2. make `setup` resolve one target directory and verify whether setup already exists there
3. add `--force` to recreate setup completely when needed
4. add `--directory` as the explicit target override
5. use current working directory as the runtime workspace selector
6. add `--vs-build` for standard Visual Studio output folders and resolve to the repo git root
7. keep PATH updates manual and print exact instructions instead of editing the user environment automatically

Phase 2:

1. decide whether to embed routing definitions and tutorials or move to a single-file publish model later if packaging becomes a problem
2. revisit whether setup needs any other flags after real usage
3. revisit whether a stronger install experience is needed later

## Design decisions

This design uses these rules:

1. `setup` verifies and creates setup in the local app folder by default.
2. `--directory` is the setup override for a different target directory.
3. Normal commands use the current working directory.
4. `--force` does a full fresh setup in the target directory.
5. Workspace initialization writes the default provider and default model.
6. Visual Studio build mode resolves to the topmost git-level workspace.
7. Runtime content stays as external files for easy editing.

## Short conclusion

Yes, the idea makes sense.

WallyCode uses a simple split: `setup` prepares one target directory, and normal commands operate on the current working directory. The future-state design keeps setup small, supports a clean reset with `--force`, supports git-root Visual Studio build behavior, and keeps runtime content easy to edit.