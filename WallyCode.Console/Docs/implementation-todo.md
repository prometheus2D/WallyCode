# Implementation Todo

Tracks every step defined in [implementation-plan.md](implementation-plan.md). Update this file as work progresses. One check-off per deliverable keeps progress visible and auditable.

Legend:

- `[ ]` not started
- `[~]` in progress
- `[x]` done

A step is only considered done when every deliverable under it is checked and the step-level "Definition of done" from `implementation-plan.md` is met (green build, green tests, no real provider in tests).

---

## Phase 1 - Setup command foundation

### Step 1.1 - Add a minimal `setup` command

- [ ] `SetupCommandOptions` added under `WallyCode.Console/Commands/`
- [ ] `SetupCommandHandler` added under `WallyCode.Console/Commands/`
- [ ] Verb registered in `Program.cs` `ParseArguments<...>`
- [ ] Target resolution: current app folder only
- [ ] Creates `wallycode.json` with default provider and model when missing
- [ ] Creates `.wallycode` folder when missing
- [ ] Prints the next commands the user should run
- [ ] Tests: first run creates files, second run is a no-op
- [ ] Step 1.1 definition of done met

### Step 1.2 - Add `--directory` override

- [ ] `--directory` option added to `SetupCommandOptions`
- [ ] Resolver prefers `--directory` over current app folder
- [ ] Path is validated or created
- [ ] Tests: override writes to the provided directory, not the app folder
- [ ] Step 1.2 definition of done met

### Step 1.3 - Add `--force`

- [ ] `--force` removes and recreates `wallycode.json` and `.wallycode`
- [ ] Without `--force`, existing setup is preserved and reported
- [ ] Tests: with and without `--force` against an already-set-up directory
- [ ] Step 1.3 definition of done met

### Step 1.4 - Add `--vs-build`

- [ ] Detect standard `bin/Debug` and `bin/Release` paths
- [ ] Walk upward to git root or `wallycode.json` / `.sln` marker
- [ ] `--vs-build` chooses that root as the setup target
- [ ] Tests: simulated build-output path resolves correctly
- [ ] Step 1.4 definition of done met

### Step 1.5 - Publish runtime assets beside the executable

- [ ] `.csproj` copies `Routing/Definitions` on build and publish
- [ ] `.csproj` copies `Tutorials` on build and publish
- [ ] Published output verified to contain both folders
- [ ] `TutorialCatalog` and routing loader still work against `AppContext.BaseDirectory`
- [ ] Short note added confirming the runtime asset layout
- [ ] Step 1.5 definition of done met

---

## Phase 2 - Working directory and remote workspace

### Step 2.1 - Working-directory model for normal commands

- [ ] Audit every command handler for workspace path resolution
- [ ] Fix any handler that assumes the app base directory
- [ ] `wallycode.json` read from the working directory
- [ ] `.wallycode` written under the working directory by default
- [ ] `--source` behavior unchanged
- [ ] Tests: commands run from a temp working directory
- [ ] Tests: project settings and runtime state both land in that working directory
- [ ] Step 2.1 definition of done met

### Step 2.2 - `--memory-root` override

- [ ] Audit `--memory-root` handling across all commands
- [ ] Runtime artifacts land under the memory root when provided
- [ ] Source workspace is untouched by the override
- [ ] Tests: memory-root override verified against source-workspace isolation
- [ ] Step 2.2 definition of done met

### Step 2.3 - Remote workspace coverage

- [ ] Scenario test: two different temp working directories from one process
- [ ] Settings stay tied to each working directory
- [ ] Runtime state stays tied to each working directory
- [ ] Install location is not modified during the test
- [ ] Step 2.3 definition of done met

---

## Phase 3 - Tutorial scenario model

### Step 3.1 - Scenario definition schema

- [ ] `TutorialScenario` type with all fields from `tutorial-automation-process.md` > "Recommended model"
- [ ] JSON loader (mirrors `RoutingDefinition` style)
- [ ] `WallyCode.Console/Scenarios/` folder created
- [ ] `.csproj` copies `Scenarios/` beside the executable
- [ ] Tests: sample scenario parses and validates
- [ ] Step 3.1 definition of done met

### Step 3.2 - First three scenarios

- [ ] `repo-review-basic.json` scenario + template
- [ ] `book-story-scaffold.json` scenario + template
- [ ] `tic-tac-toe-basic.json` scenario + template
- [ ] Tests: all three scenarios load and validate
- [ ] Step 3.2 definition of done met

### Step 3.3 - Scenario runner (mock mode)

- [ ] Runner creates temp directory
- [ ] Runner materializes workspace template
- [ ] Runner runs `setup` when the scenario requires it
- [ ] Runner executes the command sequence from the working directory
- [ ] Runner supplies scripted responses for blocked steps
- [ ] Runner captures output and file changes
- [ ] Runner evaluates success rules
- [ ] Runner writes a run summary
- [ ] Result model with `Pass`, `NeedsReview`, `Fail`
- [ ] Tests: each first-set scenario passes under the mock provider
- [ ] Step 3.3 definition of done met

### Step 3.4 - Scenario runner (attended live mode)

- [ ] Live runner uses the same scenario definitions as mock mode
- [ ] Runner mode flag: `unattended` vs `attended`
- [ ] Live-run artifact: `run.json`
- [ ] Live-run artifact: `commands.log`
- [ ] Live-run artifact: `stdout.log`
- [ ] Live-run artifact: `workspace-manifest-before.json`
- [ ] Live-run artifact: `workspace-manifest-after.json`
- [ ] Live-run artifact: `changed-files/`
- [ ] Live-run artifact: captured `wallycode.json`
- [ ] Live-run artifact: captured `.wallycode/`
- [ ] Live-run artifact: `assessment.md`
- [ ] Attended mode pauses for user input on blocked steps
- [ ] No real-provider tests added to the test project
- [ ] Step 3.4 definition of done met

---

## Phase 4 - Test project expansion

### Step 4.1 - Harden test infrastructure

- [ ] Scenario workspace builder helper
- [ ] Artifact / manifest inspection helper
- [ ] Existing `TempWorkspace` and `MockLlmProvider` kept
- [ ] No existing helper or test deleted
- [ ] Step 4.1 definition of done met

### Step 4.2 - Setup command coverage

- [ ] Default target test
- [ ] `--directory` test
- [ ] `--vs-build` test
- [ ] `--force` test
- [ ] All tests use temp directories and mock or no provider
- [ ] Step 4.2 definition of done met

### Step 4.3 - Working-directory and memory-root coverage

- [ ] Commands operate on the working directory
- [ ] Project settings read from the working directory
- [ ] Runtime artifacts land under the working directory by default
- [ ] `--memory-root` moves runtime artifacts to the override path
- [ ] Step 4.3 definition of done met

### Step 4.4 - Tutorial scenario coverage

- [ ] `repo-review-basic` mock-provider test verifies no repo files are modified
- [ ] `repo-review-basic` mock-provider test verifies output is produced
- [ ] `book-story-scaffold` mock-provider test verifies expected markdown files are created
- [ ] `book-story-scaffold` mock-provider test verifies created markdown files are non-empty
- [ ] `tic-tac-toe-basic` mock-provider test verifies expected workspace files are created or modified
- [ ] `tic-tac-toe-basic` blocked-and-resumed path is covered
- [ ] Step 4.4 definition of done met

### Step 4.5 - Failure-path coverage

- [ ] Invalid setup target
- [ ] Conflicting session definition
- [ ] Missing active session for `respond`
- [ ] Unknown provider
- [ ] Unsupported model
- [ ] Blocked session without response
- [ ] Malformed tutorial scenario metadata
- [ ] Step 4.5 definition of done met

---

## Phase 5 - Documentation follow-up

### Step 5.1 - Update `README.md`

- [ ] `setup` section added with all four flags
- [ ] Remote-workspace example using `setup` plus working directory
- [ ] Link to scenario runner usage if exposed
- [ ] Step 5.1 definition of done met

### Step 5.2 - Update tutorial markdown if commands change

- [ ] Adjusted only the parts changed by `setup` or runtime-asset rules
- [ ] No wholesale rewrite
- [ ] Step 5.2 definition of done met

---

## Global acceptance gates

These must hold continuously while working through the plan:

- [ ] Build green after every step
- [ ] All existing tests still pass after every step (per `test-project-strategy.md` > "Preserve and expand existing coverage")
- [ ] New tests added for the current step pass
- [ ] No test added that requires a real provider, token, or network call
- [ ] No existing test deleted
- [ ] Behavior matches the "Re-read first" doc sections listed for each step in `implementation-plan.md`

## Progress summary

Fill in as work proceeds:

- Phase 1 complete: not yet
- Phase 2 complete: not yet
- Phase 3 complete: not yet
- Phase 4 complete: not yet
- Phase 5 complete: not yet
- All phases complete: not yet
