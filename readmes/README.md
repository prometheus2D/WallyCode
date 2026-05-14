# WallyCode Tutorials

This folder contains test-oriented tutorials.

Each runnable tutorial follows the same contract:
- Step command: exact command to run.
- Acceptance criteria: explicit checks that must pass.
- Verification commands or artifacts: files, output, or state to inspect.

Use this to drive both manual usage and code-based test scripts.

Some tutorial flows also have matching unit tests in WallyCode.Console.Tests/Tutorials.

## Command invocation

WallyCode is not assumed to be on `PATH` yet. Run examples from the folder that contains `wallycode.exe` using `.\wallycode.exe`, or replace that prefix with the full exe path.

## Manual test defaults

The tutorials use these copy-paste defaults unless a step says otherwise:
- Repo path: `C:\src\MyRepo`
- Scratch path: `C:\src\ScratchTicTacToe`
- Default provider: `gh-copilot-claude`
- Default model: `claude-haiku-4.5`
- Session state: `<repo>\.wallycode`

Workflow commands call the configured provider and use the workspace session state.

For a clean rerun, use `setup --cleanup` for the source folder.

## High-signal conventions

- Treat each command as a state transition.
- Verify acceptance criteria before moving to the next step.
- Prefer explicit source paths when reproducing behavior.
- Tutorial tests use mocked LLM providers to verify request and response contracts deterministically.

## Read in this order

1. [Setup and providers](setup.md)
2. [Scratch project from a new folder](scratch-project.md)
3. [Ask workflow](ask.md)
4. [Act workflow](act.md)
5. [Stepwise workflows](stepwise.md)
6. [Definitions and steps](definitions.md)
7. [Development mode](development-mode.md)

## Quick chooser

- Need first-time setup: [setup.md](setup.md)
- Need a brand-new solution or program: [scratch-project.md](scratch-project.md)
- Need analysis only: [ask.md](ask.md)
- Need implementation changes: [act.md](act.md)
- Need per-iteration control: [stepwise.md](stepwise.md)
- Need to edit workflow JSON: [definitions.md](definitions.md)
- Need to run local source build: [development-mode.md](development-mode.md)

## Test discipline

- Run commands in the order shown.
- Treat every acceptance criterion as mandatory.
- If one assertion fails, stop and fix before continuing.

