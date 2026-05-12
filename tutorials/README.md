# WallyCode Tutorials

This folder contains test-oriented tutorials.

Each tutorial now follows the same contract:
- Step command: exact command to run.
- Acceptance criteria: explicit checks that must pass.
- Artifacts: files or state created/updated by that step.

Use this to drive both manual usage and code-based test scripts.

## Read in this order

1. [Setup and providers](setup.md)
2. [Ask workflow](ask.md)
3. [Act workflow](act.md)
4. [Stepwise workflows](stepwise.md)
5. [Definitions and steps](definitions.md)
6. [Development mode](development-mode.md)

## Quick chooser

- Need first-time setup: [setup.md](setup.md)
- Need analysis only: [ask.md](ask.md)
- Need implementation changes: [act.md](act.md)
- Need per-iteration control: [stepwise.md](stepwise.md)
- Need to edit workflow JSON: [definitions.md](definitions.md)
- Need to run local source build: [development-mode.md](development-mode.md)

## Test discipline

- Run commands in the order shown.
- Treat every acceptance criterion as mandatory.
- If one assertion fails, stop and fix before continuing.

