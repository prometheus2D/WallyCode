# Implementation Plan

High-level steps for implementing the designs in this `Docs/` folder. Each step is sized to be a single iterative AI prompt: small enough to implement and test in one pass, large enough to make visible progress.

## Source documents

These are the source of truth. Every implementation step must be cross-checked against them before the step is considered done.

- [setup-and-remote-workspaces.md](setup-and-remote-workspaces.md) - setup command, runtime asset model, remote workspace rules.
- [test-project-strategy.md](test-project-strategy.md) - test layering, mock-provider rule, coverage expectations.
- [tutorial-automation-process.md](tutorial-automation-process.md) - tutorial scenario model, mock and live runners, success states.

Before writing code for a step, open the referenced sections of these docs and read them in full. Do not rely on summaries in this plan alone.

## Supporting references

Use these when a step needs concrete code anchors or user-facing wording:

- [README.md](../../README.md) - user-facing command shapes, defaults, and remote-workspace examples.
- [WallyCode.Console/Tutorials/README.md](../Tutorials/README.md) - tutorial index, command-language note for `ask`/`act`/`loop`.
- [WallyCode.Console/Tutorials/repo-review.md](../Tutorials/repo-review.md) - baseline for `repo-review-basic` scenario.
- [WallyCode.Console/Tutorials/book-story.md](../Tutorials/book-story.md) - baseline for `book-story-scaffold` scenario.
- [WallyCode.Console/Tutorials/tic-tac-toe.md](../Tutorials/tic-tac-toe.md) - baseline for `tic-tac-toe-basic` scenario.

### Code anchors (existing files to mirror or modify)

- [WallyCode.Console/Program.cs](../Program.cs) - parser registration site for every command. New commands must be wired here.
- [WallyCode.Console/Commands/](../Commands/) - pattern for `*CommandOptions` + `*CommandHandler` pairs. Mirror existing files such as `ProviderCommandOptions.cs` / `ProviderCommandHandler.cs`.
- [WallyCode.Console/Commands/TutorialCatalog.cs](../Commands/TutorialCatalog.cs) - uses `AppContext.BaseDirectory` to find `Tutorials/`. Runtime-asset changes must keep this working.
- [WallyCode.Console/Routing/RoutingDefinition.cs](../Routing/RoutingDefinition.cs) - JSON loader pattern. Reuse the same System.Text.Json style for scenario loading.
- [WallyCode.Console/Routing/Definitions/](../Routing/Definitions/) - shape reference for shipped JSON under `AppContext.BaseDirectory`.
- [WallyCode.Console/Project/ProjectSettings.cs](../Project/ProjectSettings.cs) - `wallycode.json` read/write. Setup must produce output this type can round-trip.
- [WallyCode.Console/Copilot/ProviderRegistry.cs](../Copilot/ProviderRegistry.cs) - default provider and model source.
- [WallyCode.Console/Runtime/AppLogger.cs](../Runtime/AppLogger.cs) - logging style to match.
- [WallyCode.Console/WallyCode.Console.csproj](../WallyCode.Console.csproj) - where `Routing/Definitions` and `Tutorials` copy-on-build rules live or must be added.
- [WallyCode.Tests/TestInfrastructure/TempWorkspace.cs](../../WallyCode.Tests/TestInfrastructure/TempWorkspace.cs) - reuse for temp working directories in tests.
- [WallyCode.Tests/TestInfrastructure/MockLlmProvider.cs](../../WallyCode.Tests/TestInfrastructure/MockLlmProvider.cs) - the only provider allowed in tests.
- [WallyCode.Tests/TestInfrastructure/ConsoleCollectionDefinition.cs](../../WallyCode.Tests/TestInfrastructure/ConsoleCollectionDefinition.cs) - console capture pattern.
- [WallyCode.Tests/Commands/TutorialCatalogTests.cs](../../WallyCode.Tests/Commands/TutorialCatalogTests.cs) - test style to match for catalog-style tests.
- [WallyCode.Tests/Routing/RoutedRunnerTests.cs](../../WallyCode.Tests/Routing/RoutedRunnerTests.cs) - test style to match for runner-style tests.

### Repo memory notes

- `/memories/repo/runtime-assets.md` - runtime asset lookup rules and build-output walk-up markers. Read it before Phase 1 steps 1.4 and 1.5.

## How to use this plan

- Work through steps in order. Each step builds on the previous ones.
- One prompt per step. Keep the prompt focused on the step goal and the listed deliverables.
- Each step lists the exact doc sections to re-read first. Always read those before implementing.
- Every step ends with a green build and green tests before moving on.
- Tests always use mock providers. See `test-project-strategy.md` > "Core rule" and "Provider policy for tests". No step should add a test that needs a real provider, a real token, or a network call.
- Do not delete existing tests. See `test-project-strategy.md` > "Preserve and expand existing coverage".

## Current state (starting point)

- Commands already implemented: `loop`, `ask`, `act`, `prompt`, `provider`, `respond`, `shell`, `tutorial`.
- Runtime assets (`Routing/Definitions`, `Tutorials`) are read from the app base directory. See `setup-and-remote-workspaces.md` > "Runtime asset model".
- `--source` selects the working directory and `--memory-root` overrides runtime state. See `setup-and-remote-workspaces.md` > "How runtime targeting works".
- There is no `setup` command yet. See `setup-and-remote-workspaces.md` > "Proposed command shape".
- There is no scenario runner for tutorials. See `tutorial-automation-process.md` > "Two kinds of tutorial automation".

## Phase 1 - Setup command foundation

Primary reference: `setup-and-remote-workspaces.md`.

### Step 1.1 - Add a minimal `setup` command

Re-read first:

- `setup-and-remote-workspaces.md` > "Goal"
- `setup-and-remote-workspaces.md` > "Proposed command shape"
- `setup-and-remote-workspaces.md` > "Workspace initialization" (steps 4, 5, 7, 9)
- `setup-and-remote-workspaces.md` > "Design decisions" (rules 1, 5, 10)

Code anchors:

- mirror the option/handler pair style in [WallyCode.Console/Commands/ProviderCommandOptions.cs](../Commands/ProviderCommandOptions.cs) and [WallyCode.Console/Commands/ProviderCommandHandler.cs](../Commands/ProviderCommandHandler.cs)
- register the new verb in [WallyCode.Console/Program.cs](../Program.cs) alongside the existing `ParseArguments<...>` list
- read/write `wallycode.json` through [WallyCode.Console/Project/ProjectSettings.cs](../Project/ProjectSettings.cs)
- pull default provider/model from [WallyCode.Console/Copilot/ProviderRegistry.cs](../Copilot/ProviderRegistry.cs)

Goal: `wallycode setup` runs and prepares the current app folder.

Deliverables:

- new `SetupCommandOptions` and `SetupCommandHandler` (mirror the style of existing handlers under `Commands/`)
- wired into `Program.cs` parser
- target directory resolution: current app folder only for this step
- creates `wallycode.json` with default provider and model if missing (defaults per `README.md` > "Providers and Models")
- creates `.wallycode` runtime workspace folder if missing
- prints the next commands the user should run (see the example block at the end of "Workspace initialization")
- tests: command runs against a temp directory, creates expected files, leaves them alone on a second run

### Step 1.2 - Add `--directory` override

Re-read first:

- `setup-and-remote-workspaces.md` > "Proposed command shape" > "What each setup flag is for"
- `setup-and-remote-workspaces.md` > "Workspace initialization" (step 1)
- `setup-and-remote-workspaces.md` > "How runtime targeting works" (examples block)

Goal: `wallycode setup --directory <path>` targets a different folder.

Deliverables:

- `--directory` option added to `SetupCommandOptions`
- resolver uses `--directory` when provided, otherwise current app folder
- validates the path exists or can be created
- tests: directory override creates setup in the given path, not the app folder

### Step 1.3 - Add `--force`

Re-read first:

- `setup-and-remote-workspaces.md` > "Proposed command shape" > "What each setup flag is for"
- `setup-and-remote-workspaces.md` > "Workspace initialization" (steps 4, 5, 6)
- `setup-and-remote-workspaces.md` > "Design decisions" (rule 4)

Goal: `wallycode setup --force` does a full fresh setup in the resolved target directory.

Deliverables:

- `--force` removes and recreates `wallycode.json` and `.wallycode`
- without `--force`, existing setup is left alone and reported as already in place
- tests: with and without `--force` against an already-set-up directory

### Step 1.4 - Add `--vs-build`

Re-read first:

- `setup-and-remote-workspaces.md` > "Visual Studio build mode" (entire section)
- `setup-and-remote-workspaces.md` > "Design decisions" (rule 6)
- `/memories/repo/runtime-assets.md` note about walking upward to `wallycode.json`, `.sln`, or `.git` markers

Goal: when running from a Visual Studio build output, resolve the setup target to the repo git root.

Deliverables:

- detect standard `bin/Debug` or `bin/Release` paths (exact shapes listed in "Visual Studio build mode")
- walk upward to the git root (or to a `wallycode.json` / `.sln` marker)
- `--vs-build` chooses that root as the target directory
- tests: simulate a build-output path and verify the resolved target

### Step 1.5 - Ensure published builds ship runtime assets

Re-read first:

- `setup-and-remote-workspaces.md` > "Runtime asset model"
- `setup-and-remote-workspaces.md` > "Recommended implementation order" (item 7)
- `setup-and-remote-workspaces.md` > "Design decisions" (rules 7, 8)
- `/memories/repo/runtime-assets.md`

Code anchors:

- [WallyCode.Console/WallyCode.Console.csproj](../WallyCode.Console.csproj) for copy-on-build and copy-on-publish rules
- [WallyCode.Console/Commands/TutorialCatalog.cs](../Commands/TutorialCatalog.cs) - must keep working against `AppContext.BaseDirectory/Tutorials`
- [WallyCode.Console/Routing/RoutingDefinition.cs](../Routing/RoutingDefinition.cs) - must keep working against `AppContext.BaseDirectory/Routing/Definitions`
- verify the existing `bin/Debug/net8.0/Routing` and `bin/Debug/net8.0/Tutorials` layout is preserved

Goal: `Routing/Definitions` and `Tutorials` are copied beside the executable on publish.

Deliverables:

- `.csproj` updates so both folders are copied on build and publish
- verify the published output contains both folders
- short note confirming the asset layout (see also `README.md` > "Files Written")

## Phase 2 - Working directory and remote workspace behavior

Primary references: `setup-and-remote-workspaces.md` and `README.md` > "Working Against Another Repo".

### Step 2.1 - Confirm working-directory model for normal commands

Re-read first:

- `setup-and-remote-workspaces.md` > "Runtime model"
- `setup-and-remote-workspaces.md` > "How runtime targeting works"
- `setup-and-remote-workspaces.md` > "Design decisions" (rule 3)
- `README.md` > "Files Written" and "Working Against Another Repo"

Code anchors:

- every handler in [WallyCode.Console/Commands/](../Commands/) that resolves a workspace path (grep for `--source`, `memoryRoot`, `BaseDirectory`)
- [WallyCode.Console/Project/ProjectSettings.cs](../Project/ProjectSettings.cs) for the `wallycode.json` lookup path
- [WallyCode.Console/Routing/RoutedSession.cs](../Routing/RoutedSession.cs) for `.wallycode` write paths

Goal: normal commands read `wallycode.json` from the current working directory and write `.wallycode` under it by default.

Deliverables:

- audit existing command handlers and project settings lookup
- fix any place that assumes the app base directory instead of the working directory
- keep `--source` behavior unchanged (per `README.md` > "Working Against Another Repo")
- tests: run commands from a temp working directory and verify project settings and runtime state land there

### Step 2.2 - Confirm `--memory-root` override

Re-read first:

- `setup-and-remote-workspaces.md` > "Runtime model" (note on `--memory-root`)
- `README.md` > "Working Against Another Repo" (meaning of `--memory-root`)
- `test-project-strategy.md` > "Setup and remote workspace coverage" > "memory-root override behavior"

Goal: `--memory-root` moves runtime state out of the working directory without affecting the source workspace.

Deliverables:

- audit `--memory-root` handling across commands
- tests: runtime artifacts appear under the explicit memory root and not under the working directory

### Step 2.3 - Remote workspace coverage

Re-read first:

- `setup-and-remote-workspaces.md` > "Remote usage examples"
- `test-project-strategy.md` > "Setup and remote workspace coverage" > "remote workspace behavior"

Goal: one installed executable operates cleanly against multiple working directories.

Deliverables:

- scenario test: run commands against two different temp working directories from the same process
- verify settings and runtime state stay tied to each working directory
- verify the install location is not modified

## Phase 3 - Tutorial scenario model

Primary reference: `tutorial-automation-process.md`.

### Step 3.1 - Define the scenario definition schema

Re-read first:

- `tutorial-automation-process.md` > "Principle"
- `tutorial-automation-process.md` > "Recommended model"
- `tutorial-automation-process.md` > "Keep scenario definitions simple"
- `tutorial-automation-process.md` > "Runtime model alignment"

Code anchors:

- follow the JSON-shape and loader style of [WallyCode.Console/Routing/RoutingDefinition.cs](../Routing/RoutingDefinition.cs) and [WallyCode.Console/Routing/Definitions/](../Routing/Definitions/)
- place scenarios under a new `WallyCode.Console/Scenarios/` folder alongside `Routing/Definitions/` and `Tutorials/`
- add a copy-on-build rule in [WallyCode.Console/WallyCode.Console.csproj](../WallyCode.Console.csproj) matching the Routing/Tutorials rule from step 1.5

Goal: a small data model for tutorial scenarios that both mock tests and live runs can share.

Deliverables:

- new type `TutorialScenario` with the exact fields listed in "Recommended model":
  `name`, `tutorial`, `category`, `requiresSetup`, `setupTarget`, `workingDirectory`, `workspaceTemplate`, `commands`, `responses`, `expectedFilesCreated`, `expectedFilesModified`, `expectedOutputContains`, `expectedSessionArtifacts`, `successRules`
- JSON loader for scenario files
- a `Scenarios/` folder for shipped scenario definitions
- tests: load a sample scenario and validate parsed fields

### Step 3.2 - First three scenarios

Re-read first:

- `tutorial-automation-process.md` > "First scenario set"
- `tutorial-automation-process.md` > "Workspace strategy"
- `tutorial-automation-process.md` > "What tutorials cover today"

Goal: ship one scenario per current tutorial.

Deliverables:

- `repo-review-basic.json` - analysis-only `ask` flow, no files changed
- `book-story-scaffold.json` - `act` flow that creates markdown files
- `tic-tac-toe-basic.json` - default `loop`, expected workspace files appear
- tiny workspace template per scenario (sizes described in "Workspace strategy")
- tests: scenarios load and validate

### Step 3.3 - Scenario runner (mock mode)

Re-read first:

- `tutorial-automation-process.md` > "Execution modes" > "Unattended execution"
- `tutorial-automation-process.md` > "Workspace strategy" (runner flow steps 1-8)
- `tutorial-automation-process.md` > "Mock-provider test cases"
- `tutorial-automation-process.md` > "Success model"
- `test-project-strategy.md` > "Tutorial automation coverage in tests"

Code anchors:

- [WallyCode.Console/Routing/RoutedRunner.cs](../Routing/RoutedRunner.cs) for the existing runner pattern
- [WallyCode.Tests/TestInfrastructure/MockLlmProvider.cs](../../WallyCode.Tests/TestInfrastructure/MockLlmProvider.cs) for scripted responses
- [WallyCode.Tests/TestInfrastructure/TempWorkspace.cs](../../WallyCode.Tests/TestInfrastructure/TempWorkspace.cs) for temp directory handling

Goal: run a scenario end-to-end using a mock provider.

Deliverables:

- runner that follows the 8-step flow in "Workspace strategy": create temp directory, materialize template, run `setup` if required, run commands from the working directory, supply scripted responses for blocked steps, capture output and file changes, evaluate rules, write a run summary
- result model with `Pass`, `NeedsReview`, `Fail` (per "Success model")
- tests: each first-set scenario passes under the mock provider

### Step 3.4 - Scenario runner (attended live mode)

Re-read first:

- `tutorial-automation-process.md` > "Execution modes" > "Attended execution"
- `tutorial-automation-process.md` > "Live tutorial runs"
- `tutorial-automation-process.md` > "What to capture"
- `tutorial-automation-process.md` > "Success model"
- `test-project-strategy.md` > "Boundary between tests and live runs"

Goal: run the same scenarios with a real provider, stepping through blocked states.

Deliverables:

- runner mode flag: `unattended` vs `attended`
- live-run artifact capture per "What to capture": `run.json`, `commands.log`, `stdout.log`, `workspace-manifest-before.json`, `workspace-manifest-after.json`, `changed-files/`, `wallycode.json`, `.wallycode/` artifacts, `assessment.md`
- attended mode pauses for user input on blocked steps
- no new tests that depend on a real provider - live runs stay outside the test project (see `test-project-strategy.md` > "Boundary between tests and live runs")

## Phase 4 - Test project expansion

Primary reference: `test-project-strategy.md`.

### Step 4.1 - Harden test infrastructure

Re-read first:

- `test-project-strategy.md` > "Naming and organization"
- `test-project-strategy.md` > "Test data strategy"
- `test-project-strategy.md` > "Recommended expansion order" > "Phase 1"

Code anchors:

- [WallyCode.Tests/TestInfrastructure/](../../WallyCode.Tests/TestInfrastructure/) - extend, do not replace
- [WallyCode.Tests/TestInfrastructure/TestDefinitions.cs](../../WallyCode.Tests/TestInfrastructure/TestDefinitions.cs) for shared test fixture patterns
- [WallyCode.Tests/Commands/TutorialCatalogTests.cs](../../WallyCode.Tests/Commands/TutorialCatalogTests.cs) and [WallyCode.Tests/Routing/RoutedRunnerTests.cs](../../WallyCode.Tests/Routing/RoutedRunnerTests.cs) for naming and assertion style

Goal: shared helpers that keep new tests small and consistent.

Deliverables:

- scenario workspace builder helper
- artifact / manifest inspection helper
- keep existing `TempWorkspace` and `MockLlmProvider` patterns
- no deletion of existing helpers or tests (see "Preserve and expand existing coverage")

### Step 4.2 - Setup command coverage

Re-read first:

- `test-project-strategy.md` > "Setup and remote workspace coverage" > "setup target resolution"
- `test-project-strategy.md` > "What tests should assert"
- `setup-and-remote-workspaces.md` > "Workspace initialization" (full step list)

Goal: deterministic tests for setup target resolution.

Deliverables:

- default target, `--directory`, `--vs-build`, `--force` tests
- all in temp directories with no real provider

### Step 4.3 - Working-directory and memory-root coverage

Re-read first:

- `test-project-strategy.md` > "Setup and remote workspace coverage" > "working-directory behavior" and "memory-root override behavior"
- `test-project-strategy.md` > "Test design principles"

Goal: tests that pin the runtime model.

Deliverables:

- commands operate on the working directory
- project settings read from the working directory
- runtime artifacts land under the working directory by default
- `--memory-root` moves runtime artifacts to the override path

### Step 4.4 - Tutorial scenario coverage

Re-read first:

- `test-project-strategy.md` > "Tutorial automation coverage in tests"
- `tutorial-automation-process.md` > "Mock-provider test cases" (all three tutorial test sections)
- `tutorial-automation-process.md` > "First scenario set"

Goal: a mock-provider test per first-set scenario.

Deliverables:

- `repo-review-basic` - no repo files modified, output produced
- `book-story-scaffold` - expected markdown files created and non-empty
- `tic-tac-toe-basic` - expected workspace files created or modified, blocked-and-resumed path works

### Step 4.5 - Failure-path coverage

Re-read first:

- `test-project-strategy.md` > "Failure-path coverage"
- `test-project-strategy.md` > "Console and artifact verification"

Goal: clear errors for bad inputs.

Deliverables (from "Failure-path coverage"):

- invalid setup target
- conflicting session definition
- missing active session for `respond`
- unknown provider / unsupported model
- blocked session without response
- malformed tutorial scenario metadata

## Phase 5 - Documentation follow-up

Only after the above steps are implemented and passing.

### Step 5.1 - Update `README.md`

Re-read first:

- `setup-and-remote-workspaces.md` > "Proposed command shape"
- `setup-and-remote-workspaces.md` > "Remote usage examples"
- `setup-and-remote-workspaces.md` > "Short conclusion"

Deliverables:

- short `setup` section with the four flags (`--directory`, `--force`, `--vs-build`, default)
- short remote-workspace example using `setup` plus working directory
- link to scenario runner usage if exposed as a command

### Step 5.2 - Update tutorial markdown if commands change

Re-read first:

- `tutorial-automation-process.md` > "How tutorial automation should relate to setup"
- existing tutorials under `WallyCode.Console/Tutorials/`

Deliverables:

- adjust only the parts that change because of `setup` or runtime-asset rules
- do not rewrite tutorials wholesale

## Definition of done per step

A step is done when:

- the build is green
- all existing tests still pass (per `test-project-strategy.md` > "Preserve and expand existing coverage")
- the new tests added for that step pass
- behavior matches the corresponding sections listed under "Re-read first" for that step
- no real provider, token, or network call was introduced in the test project (per `test-project-strategy.md` > "Provider policy for tests")

## Out of scope for this plan

Drawn directly from `setup-and-remote-workspaces.md` > "Design decisions" and `test-project-strategy.md` > "Boundary between tests and live runs":

- editing the user's PATH automatically (rule 9)
- bundling runtime assets into a single-file executable (rule 8)
- any live-provider assertion inside the test project
- rewriting existing tutorials beyond what the runtime model requires

## Cross-reference quick map

Use this map when a step feels ambiguous.

- command shape and flags -> `setup-and-remote-workspaces.md` > "Proposed command shape"
- setup step-by-step behavior -> `setup-and-remote-workspaces.md` > "Workspace initialization"
- Visual Studio build detection -> `setup-and-remote-workspaces.md` > "Visual Studio build mode"
- runtime asset copying -> `setup-and-remote-workspaces.md` > "Runtime asset model"
- design rules and trade-offs -> `setup-and-remote-workspaces.md` > "Design decisions"
- mock-only test rule -> `test-project-strategy.md` > "Core rule" and "Provider policy for tests"
- keeping existing tests -> `test-project-strategy.md` > "Preserve and expand existing coverage"
- test layers -> `test-project-strategy.md` > "Recommended test layers"
- scenario fields -> `tutorial-automation-process.md` > "Recommended model"
- scenario runner flow -> `tutorial-automation-process.md` > "Workspace strategy"
- execution modes -> `tutorial-automation-process.md` > "Execution modes"
- live run artifacts -> `tutorial-automation-process.md` > "What to capture"
- success states -> `tutorial-automation-process.md` > "Success model"
