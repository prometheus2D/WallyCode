# Tutorial Automation Process

## Goal

Use tutorials as executable product scenarios that follow the same runtime model as normal WallyCode usage.

This document assumes the setup and remote workspace design comes first.
Tutorial automation should build on that design instead of inventing a separate execution model.

The goal is simple:

1. keep tutorials readable for humans
2. run tutorials automatically with mock providers for regression coverage
3. run tutorials live against real providers to observe what actually happens

The process should stay small, direct, and consistent with normal command execution.

## Runtime model alignment

Tutorial automation should follow the same rules as the setup and remote workspace design.

Use these terms consistently:

- install location: the folder that contains the WallyCode executable and companion runtime assets
- setup target: the folder prepared by `setup`
- working directory: the directory normal WallyCode commands operate on
- runtime workspace: the `.wallycode` folder under the working directory, or the folder provided by `--memory-root`
- project settings: `wallycode.json` stored in the working directory

Tutorial automation should not introduce a different workspace model.

The rule is:

- `setup` prepares one target directory
- tutorial commands run from the working directory they are meant to exercise
- runtime state stays under that working directory by default
- `--memory-root` remains an override when a scenario needs it

That keeps tutorial runs consistent with real usage.

## What tutorials cover today

Current tutorials cover the three core workflow shapes:

- `repo-review` -> analysis-only `ask`
- `book-story` -> direct file-editing `act`
- `tic-tac-toe` -> iterative default `loop`

That is enough to represent the basic product use cases.

Current gaps:

- `prompt`
- `provider`
- `shell`
- `setup`
- explicit remote-workspace walkthroughs

Those gaps do not block tutorial automation. They only limit coverage breadth.

## Principle

A tutorial should define one small, realistic scenario.

Each scenario should answer only these questions:

- what setup target or working directory do we start with
- does the scenario require setup first
- what command or commands do we run from the working directory
- do we expect file changes
- what artifacts should exist after the run
- what counts as success

Anything beyond that is optional.

## How tutorial automation should relate to setup

Tutorial automation should assume the setup flow exists and should use it the same way a real user would.

That means a tutorial scenario may have one of these starting states:

1. pre-setup workspace: the scenario begins by running `setup` against a target directory
2. already-set-up workspace: the scenario begins with `wallycode.json` and runtime workspace already present
3. explicit runtime override: the scenario uses `--memory-root` for runtime state outside the working directory

For most tutorial scenarios, the simplest path is:

1. create a temporary working directory
2. run `setup` there if the scenario depends on setup behavior
3. change into that working directory
4. run the tutorial commands there
5. inspect `wallycode.json`, `.wallycode`, console output, and changed files

This matches the intended product model: setup prepares a directory, then normal commands operate from that directory.

## Execution modes

Tutorial automation should support both complete runs and paused runs.

### 1. Unattended execution

Use this mode when the scenario should run end to end without human intervention.

In this mode:

- setup runs if required by the scenario
- tutorial commands run from the working directory
- scripted responses are supplied automatically when the scenario expects a blocked step
- the run continues until completion, failure, or an unexpected blocked state

This is the normal mode for mock-provider regression tests.
It can also be used for live runs when the scenario includes predefined responses.

### 2. Attended execution

Use this mode when the scenario should pause and let a user decide how to continue.

In this mode:

- setup runs if required by the scenario
- tutorial commands run from the working directory
- if the run blocks, the current state is shown to the user
- the user provides the next response manually
- the run continues step by step until completion, failure, or user stop

This mode is mainly for live tutorial review.
It allows a tutorial to be exercised in real time while still using the same scenario definition and workspace model.

## Two kinds of tutorial automation

We need both of these.

### 1. Test automation with mock providers

Purpose:

- fast regression coverage
- deterministic command flow checks
- validation of tutorial scenarios in CI

This is where we verify that tutorials still map to valid command behavior.

Use mock providers to simulate provider responses and scripted blocking steps.

This should be the default automated test path.

### 2. Live tutorial runs

Purpose:

- observe real provider behavior
- see whether a tutorial still works in practice
- capture outputs, file changes, and blocked states

This is not a strict deterministic test. It is a live scenario run.

Use it to inspect real outcomes, not to enforce brittle exact-output assertions.

Live runs may be either unattended or attended depending on whether the scenario should continue automatically or pause for user input.

## Recommended model

Keep the model minimal.

Each tutorial should have:

1. a markdown document for humans
2. a small scenario definition for automation

The scenario definition should contain only the fields needed to run and evaluate the scenario.

Suggested fields:

- `name`
- `tutorial`
- `category`
- `requiresSetup`
- `setupTarget`
- `workingDirectory`
- `workspaceTemplate`
- `commands`
- `responses`
- `expectedFilesCreated`
- `expectedFilesModified`
- `expectedOutputContains`
- `expectedSessionArtifacts`
- `successRules`

That is enough for both mock-driven tests and live runs.

The `responses` field should support both:

- scripted responses for unattended execution
- no scripted response, which means an attended run pauses for user input when blocked

## Keep scenario definitions simple

Do not try to encode every possible outcome.

A scenario definition should describe:

- the starting directory state
- whether setup is part of the scenario
- the intended command path from the working directory
- the minimum expected result

It should not try to fully model LLM behavior.

## Workspace strategy

Run every tutorial in an isolated temporary directory.

That directory should be treated as the working directory unless the scenario explicitly tests remote targeting behavior.

Use a tiny template per scenario.

Examples:

- `repo-review`: a small sample repo with a few source files
- `book-story`: an empty or lightly seeded markdown workspace
- `tic-tac-toe`: an empty web folder or tiny starter app

Runner flow:

1. create temp directory
2. materialize the workspace template
3. run `setup` if the scenario requires it
4. run tutorial commands from the working directory
5. continue automatically or pause for user input based on the execution mode
6. capture outputs and file changes
7. evaluate against simple rules
8. write a run summary

## Mock-provider test cases

Tutorial tests should exist for every automated tutorial scenario.

These tests should use mock providers and verify the command flow, not provider intelligence.

What mock-provider tutorial tests should verify:

- the tutorial scenario can be loaded
- the setup step is run or skipped correctly
- the command sequence is valid for the working directory model
- the expected provider calls occur
- expected files are created or unchanged
- expected session artifacts are written in the correct runtime workspace
- blocked flows can be resumed with scripted `respond` steps

Examples:

### repo-review tutorial test

Verify:

- setup state is correct for the scenario
- `ask` or `loop --definition ask` runs successfully from the working directory
- no repo files are modified
- output is produced
- runtime artifacts exist if session-based

### book-story tutorial test

Verify:

- setup state is correct for the scenario
- `act` flow runs successfully from the working directory
- expected markdown files are created
- files are non-empty

### tic-tac-toe tutorial test

Verify:

- setup state is correct for the scenario
- default `loop` starts and advances from the working directory
- expected workspace files are created or modified
- blocked states can be simulated and resumed

These tests should be stable and fast enough for normal test runs.

## Live tutorial runs

Live runs should use the same scenario definitions.

The difference is only the execution mode.

Instead of a mock provider, the runner uses a real provider and records what happened.

Live runs should support both:

- unattended end-to-end execution
- attended step-by-step execution with user input when blocked

Live runs should capture:

- whether setup was run
- commands executed
- working directory used
- whether the run was unattended or attended
- console output
- files created or modified
- `wallycode.json`
- `.wallycode` artifacts
- final scenario assessment

Live runs should not depend on exact wording.

They should evaluate broad outcomes such as:

- did setup complete when required
- did the command complete
- did the expected files appear
- did the run block
- if blocked, was the reason understandable
- if attended, did the user have a clear path to continue
- did the result look usable

## Success model

Use three result states:

- `Pass`
- `NeedsReview`
- `Fail`

Use them like this:

- `Pass`: required expectations were met
- `NeedsReview`: run completed but outcome was ambiguous, low quality, or unexpectedly blocked
- `Fail`: setup or command flow broke, or required expectations were not met

This works for both mock tests and live runs.

## What to capture

Keep artifacts small and useful.

Recommended outputs:

- `run.json` - structured summary
- `commands.log` - executed commands
- `stdout.log` - console output
- `workspace-manifest-before.json`
- `workspace-manifest-after.json`
- `changed-files/` - copies of changed files
- `wallycode.json` if produced or modified
- `.wallycode/` artifacts if produced
- `assessment.md` - short human-readable result

Prefer manifests and changed-file copies over full workspace snapshots unless full snapshots are needed.

## Minimal implementation path

Implement this in the smallest useful order.

### Phase 1: align with setup and workspace rules

- define tutorial scenarios in terms of setup target and working directory
- keep scenario metadata minimal
- document expected success rules
- document unattended and attended execution modes

### Phase 2: add mock-provider tests

- run tutorial scenarios in temp directories
- use setup behavior where the scenario requires it
- use mock providers to simulate responses
- verify command flow and artifacts

This is the main regression layer.

### Phase 3: add live tutorial runner

- run the same scenarios with a real provider
- support unattended end-to-end runs and attended step-by-step runs
- capture setup results, outputs, and changed files
- emit `Pass`, `NeedsReview`, or `Fail`

This is the observational layer.

## First scenario set

Start with one scenario per current tutorial.

### `repo-review-basic`

Purpose:
- verify analysis-only workflow

Checks:
- setup state is correct for the scenario
- no repo files changed
- output produced
- session artifacts exist if loop-based

### `book-story-scaffold`

Purpose:
- verify direct file creation workflow

Checks:
- setup state is correct for the scenario
- outline and chapter files created
- files are non-empty

### `tic-tac-toe-build`

Purpose:
- verify iterative feature workflow

Checks:
- setup state is correct for the scenario
- app files created or modified
- progress artifacts captured
- blocked states can be recorded and resumed

## Recommendation

Use tutorials as lightweight executable scenarios that follow the same setup and working-directory model as the rest of WallyCode.

For normal automated testing, use mock providers.
For real-time validation, run the same scenarios live and capture what happened.

Support both unattended end-to-end runs and attended step-by-step runs.
Keep the scenario model small, the checks broad, and the artifacts useful.
That gives us practical tutorial automation without overengineering it.
