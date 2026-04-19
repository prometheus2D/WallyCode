# Tutorial Automation Process

## Goal

Use tutorials as executable product scenarios.

We want two things from the same tutorial set:

1. readable guidance for users
2. repeatable automated coverage for the core workflows

The process must stay simple. Tutorials should be easy to maintain, easy to run, and useful for both regression testing and real-time observation.

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

- what workspace do we start with
- what command or commands do we run
- do we expect file changes
- what artifacts should exist after the run
- what counts as success

Anything beyond that is optional.

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
- `workspaceTemplate`
- `commands`
- `responses`
- `expectedFilesCreated`
- `expectedFilesModified`
- `expectedOutputContains`
- `expectedSessionArtifacts`
- `successRules`

That is enough for both mock-driven tests and live runs.

## Keep scenario definitions simple

Do not try to encode every possible outcome.

A scenario definition should describe:

- the starting workspace
- the intended command path
- the minimum expected result

It should not try to fully model LLM behavior.

## Workspace strategy

Run every tutorial in an isolated temporary workspace.

Use a tiny template per scenario.

Examples:

- `repo-review`: a small sample repo with a few source files
- `book-story`: an empty or lightly seeded markdown workspace
- `tic-tac-toe`: an empty web folder or tiny starter app

Runner flow:

1. create temp workspace
2. materialize template
3. run tutorial commands
4. capture outputs and file changes
5. evaluate against simple rules
6. write a run summary

## Mock-provider test cases

Tutorial tests should exist for every automated tutorial scenario.

These tests should use mock providers and verify the command flow, not provider intelligence.

What mock-provider tutorial tests should verify:

- the tutorial scenario can be loaded
- the command sequence is valid
- the expected provider calls occur
- expected files are created or unchanged
- expected session artifacts are written
- blocked flows can be resumed with scripted `respond` steps

Examples:

### repo-review tutorial test

Verify:

- `ask` or `loop --definition ask` runs successfully
- no repo files are modified
- output is produced
- runtime artifacts exist if session-based

### book-story tutorial test

Verify:

- `act` flow runs successfully
- expected markdown files are created
- files are non-empty

### tic-tac-toe tutorial test

Verify:

- default `loop` starts and advances
- expected workspace files are created or modified
- blocked states can be simulated and resumed

These tests should be stable and fast enough for normal test runs.

## Live tutorial runs

Live runs should use the same scenario definitions.

The difference is only the execution mode.

Instead of a mock provider, the runner uses a real provider and records what happened.

Live runs should capture:

- commands executed
- console output
- files created or modified
- `.wallycode` artifacts
- final scenario assessment

Live runs should not depend on exact wording.

They should evaluate broad outcomes such as:

- did the command complete
- did the expected files appear
- did the run block
- if blocked, was the reason understandable
- did the result look usable

## Success model

Use three result states:

- `Pass`
- `NeedsReview`
- `Fail`

Use them like this:

- `Pass`: required expectations were met
- `NeedsReview`: run completed but outcome was ambiguous, low quality, or unexpectedly blocked
- `Fail`: command flow broke or required expectations were not met

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
- `.wallycode/` artifacts if produced
- `assessment.md` - short human-readable result

Prefer manifests and changed-file copies over full workspace snapshots unless full snapshots are needed.

## Minimal implementation path

Implement this in the smallest useful order.

### Phase 1: define scenarios

- map each current tutorial to one scenario
- keep scenario metadata minimal
- document expected success rules

### Phase 2: add mock-provider tests

- run tutorial scenarios in temp workspaces
- use mock providers to simulate responses
- verify command flow and artifacts

This is the main regression layer.

### Phase 3: add live tutorial runner

- run the same scenarios with a real provider
- capture outputs and changed files
- emit `Pass`, `NeedsReview`, or `Fail`

This is the observational layer.

## First scenario set

Start with one scenario per current tutorial.

### `repo-review-basic`

Purpose:
- verify analysis-only workflow

Checks:
- no repo files changed
- output produced
- session artifacts exist if loop-based

### `book-story-scaffold`

Purpose:
- verify direct file creation workflow

Checks:
- outline and chapter files created
- files are non-empty

### `tic-tac-toe-build`

Purpose:
- verify iterative feature workflow

Checks:
- app files created or modified
- progress artifacts captured
- blocked states can be recorded and resumed

## Recommendation

Use tutorials as lightweight executable scenarios.

For normal automated testing, use mock providers.
For real-time validation, run the same scenarios live and capture what happened.

Keep the scenario model small, the checks broad, and the artifacts useful.
That gives us practical tutorial automation without overengineering it.
