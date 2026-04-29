# WallyCode Readmes

Focused guides for common WallyCode tasks. The root README stays short; this folder is for workflows and setup notes that need examples.

## Start here

- [Setup and providers](setup.md) - initialize a target repo and choose provider/model defaults.
- [Development mode](development-mode.md) - run the current source build while modifying WallyCode itself.
- [Ask workflow](ask.md) - answer questions against a repo without intending to edit files.
- [Act workflow](act.md) - run an implementation-oriented workflow that may change files.
- [Definitions and steps](definitions.md) - understand and edit workflow definitions, shared steps, and keywords.
- [Stepwise workflows](stepwise.md) - run one routed iteration at a time and respond to blocked sessions.

## Core idea

WallyCode runs a routed workflow session against a source repo. `--source` selects the repo, `.wallycode` stores runtime state, and `--memory-root` lets you isolate a different session root for experiments or parallel runs.

`ask` and `act` are shortcut commands. The general form is `loop --definition <name>`.
