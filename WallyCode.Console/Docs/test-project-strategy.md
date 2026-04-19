# Test Project Strategy

## Goal

Define how the test project should evolve to support the setup, remote workspace, and tutorial automation model without losing the coverage the test project already provides.

The rule is strict:

- the test project always uses mock providers
- no tokens are used in tests
- no raw CLI calls to external LLM tools are allowed in tests
- no test should depend on external provider availability

This keeps the test suite deterministic, fast, safe, and suitable for normal local runs and CI.

## Scope

This document applies to the test project and any future automated test coverage added for:

- existing command behavior already covered by tests
- setup behavior
- working-directory behavior
- remote workspace behavior
- routing and session behavior
- tutorial automation scenarios
- provider selection and model resolution logic

It does not describe live tutorial runs.
Live runs belong outside the test project because they are observational runs, not deterministic automated tests.

## Core rule

The test project validates product logic, command flow, file effects, and runtime artifacts.
It does not validate real LLM quality or real provider integration.

That means tests should verify:

- command parsing and command flow
- setup target resolution
- working-directory behavior
- runtime workspace behavior
- session persistence and resume behavior
- file creation and modification rules
- provider selection logic
- tutorial scenario execution rules
- existing behaviors already covered by the current test suite

Tests should not verify:

- real provider availability
- real token configuration
- real external CLI behavior
- exact wording from a live model

## Preserve and expand existing coverage

The current test suite already covers important behavior.
That coverage should be retained.

The rule is:

- do not replace existing tests with broader but weaker tests
- do not remove narrow deterministic tests just because a higher-level scenario test exists
- do not move deterministic coverage out of the test project into live runs
- add new tests around the existing suite instead of collapsing the suite into only scenario tests

Existing tests should remain the base regression layer.
New tests should extend that layer to cover setup, remote workspaces, and tutorial automation.

In practice this means:

- keep current unit and command-level tests
- strengthen them where the runtime model changes
- add new scenario tests without deleting lower-level tests
- preserve current routing, session, tutorial catalog, and command coverage while expanding it

## Provider policy for tests

All provider-backed tests must use mock providers.

Allowed in tests:

- in-memory mock providers
- scripted provider responses
- scripted provider failures
- scripted blocked-state responses

Not allowed in tests:

- GitHub Copilot CLI execution
- shelling out to provider CLIs
- environment-token dependency
- network dependency
- any test that requires a real account or login state

If a scenario needs live validation, it should be handled by a separate live-run process outside the test project.

## Test design principles

Keep tests aligned with the runtime model.

The runtime model is:

- `setup` prepares one target directory
- normal commands operate on the working directory
- runtime state lives under the working directory by default
- `--memory-root` is an explicit override

Tests should be written around those rules.

Each test should make the directory model obvious:

- what is the install location if relevant
- what is the setup target
- what is the working directory
- where runtime artifacts should be written

When existing tests already cover behavior in a simpler way, keep them and add only the extra assertions needed for the new model.

## Recommended test layers

Keep the test project simple by using a small number of clear layers.

### 1. Unit tests

Use unit tests for isolated logic such as:

- path resolution
- scenario loading
- tutorial metadata parsing
- provider selection rules
- routing definition loading
- session serialization

These should avoid unnecessary filesystem setup unless the logic requires it.

Existing unit tests should remain in place and be expanded only when the underlying logic grows.

### 2. Command-level tests

Use command-level tests for:

- setup command behavior
- prompt, ask, act, loop, respond, provider, shell behavior where practical
- working-directory and memory-root behavior
- file and artifact creation

These tests should use temporary directories and mock providers.

Existing command-level tests should remain the main deterministic coverage for command behavior.
Scenario tests should not replace them.

### 3. Scenario tests

Use scenario tests for end-to-end product flows inside the test project.

Examples:

- setup a workspace and run a tutorial scenario
- start a loop, block, respond, and continue
- run an analysis-only scenario and verify no repo files changed

Scenario tests should still use mock providers only.

This is the highest-level deterministic coverage layer in the test project.
It should sit on top of the existing lower-level tests, not replace them.

## Tutorial automation coverage in tests

Tutorial automation tests should be added as scenario tests.

Each automated tutorial scenario should have a deterministic test version that uses a mock provider script.

Those tests should verify:

- scenario setup is correct
- setup is run or skipped correctly
- commands run from the intended working directory
- expected provider calls occur in order
- expected files are created, modified, or left unchanged
- expected `.wallycode` artifacts are written
- blocked flows can be resumed with scripted responses

The test project should treat tutorials as executable scenarios, but only in mock-driven form.

These tutorial scenario tests should expand coverage, not replace existing tutorial catalog or command tests.

## Setup and remote workspace coverage

Tests should expand to cover the setup and remote workspace rules directly.

Recommended coverage:

### setup target resolution

Verify:

- default setup target behavior
- `--directory` override behavior
- `--vs-build` resolution behavior
- `--force` reset behavior

### working-directory behavior

Verify:

- normal commands operate on the working directory
- project settings are read from the working directory
- runtime artifacts are written under the working directory by default

### memory-root override behavior

Verify:

- runtime artifacts move to the explicit memory root
- source workspace behavior remains unchanged

### remote workspace behavior

Verify:

- one executable can operate against different working directories
- settings and runtime state stay with the selected workspace model

These tests should be added in addition to existing routing and command tests.

## What tests should assert

Prefer broad, stable assertions.

Good assertions:

- expected files exist
- expected files do not exist
- expected files changed or did not change
- expected session files were written
- expected provider requests were made
- expected command result state was reached

Avoid brittle assertions when they are not necessary.

Avoid depending on:

- exact timestamps
- exact formatting unless formatting is the feature under test
- exact long-form generated text when a broader assertion is enough

When an existing test already asserts a stable narrow behavior, keep that assertion.
Do not weaken it just to make tests more generic.

## Console and artifact verification

When command behavior matters, tests may capture console output.

Use console assertions only for stable, meaningful signals such as:

- command completed
- command blocked
- setup already exists
- tutorial not found

Prefer filesystem and structured artifact assertions over long console transcript assertions.

Filesystem and runtime artifact checks are usually more stable and more valuable.

If an existing test already uses console assertions effectively, keep it unless there is a clear reason to replace it.

## Failure-path coverage

The test project should include failure-path tests for important flows.

Examples:

- invalid setup target
- conflicting session definition
- missing active session for `respond`
- unknown provider
- unsupported model
- blocked session without response
- malformed tutorial scenario metadata

These tests should still use mock providers or no provider at all.

Existing failure-path tests should be preserved and expanded where the new runtime model introduces new failure cases.

## Test data strategy

Keep test data small and local.

Use:

- temporary directories for workspace state
- tiny sample repos or templates
- small markdown files
- small routing fixtures when needed
- scripted mock-provider outputs

Do not add large fixture trees unless they are clearly necessary.

When existing fixtures already cover a behavior well, reuse them before adding new fixture structures.

## Naming and organization

Organize tests by behavior, not by implementation detail.

Suggested areas:

- `Commands/`
- `Routing/`
- `Project/` or `Workspace/`
- `Tutorials/` or `Scenarios/`
- `TestInfrastructure/`

Keep shared helpers in `TestInfrastructure/`.

Examples of useful shared helpers:

- temp workspace creation
- mock provider scripting
- console capture helpers
- file manifest helpers
- scenario workspace builders

New helpers should reduce duplication without hiding test intent.

## Recommended expansion order

Expand the test project in this order:

### Phase 1: preserve and strengthen infrastructure

- keep existing mock provider support simple and reusable
- keep existing temp workspace and helper patterns where they already work
- add helpers for artifact inspection and scenario setup only where they reduce duplication

### Phase 2: preserve current coverage while adding setup and workspace rules

- keep existing routing, session, tutorial, and command tests
- add tests for setup target resolution
- add tests for working-directory behavior
- add tests for memory-root behavior
- add tests for remote workspace behavior

### Phase 3: add tutorial scenario tests

- add one deterministic scenario test per tutorial
- verify command flow and artifacts with mock providers
- cover blocked and resumed flows where relevant
- keep lower-level tutorial and command tests in place

### Phase 4: fill failure-path gaps

- add tests for invalid inputs and broken states
- verify errors are clear and stable
- preserve existing failure-path coverage

## Boundary between tests and live runs

Keep this boundary strict.

Inside the test project:

- deterministic tests only
- mock providers only
- no external provider calls

Outside the test project:

- live tutorial runs
- real provider observation
- manual or semi-automated quality review

This separation prevents the test suite from becoming slow, flaky, or environment-dependent.

## Recommendation

Upgrade the test project around the same runtime rules used by setup, remote workspaces, and tutorial automation.

Use mock providers for every provider-backed test.
Never require tokens, external CLI calls, or live LLM access in the test project.

Retain the scope of the current test suite and expand it.
Treat existing tests as the base regression layer, add new deterministic coverage on top, and keep live provider runs outside the test project.
