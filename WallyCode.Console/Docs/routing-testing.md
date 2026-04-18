# WallyCode Routing Testing

This document defines the testing model for the routing system.

Related documents:

- `routing.md`
- `routing-examples.md`
- `requirements-gathering.md`
- `task-generation.md`
- `task-execution.md`

---

## Purpose

This document defines how the routing engine is tested at definition, step, logical-unit workflow, and full routed workflow levels.

The goal is to verify that routed execution behavior is deterministic when provider output is controlled.

These tests verify the contracts defined in `routing.md` and should share one concrete mock provider implementation across provider-backed tests.

---

## Testing Scope

The automated test model is layered.

Full routed workflow coverage is still required, but it is not the only required test shape.

Tests should be organized into:

- definition validation tests that reject invalid routing definitions without invoking a provider
- step tests that execute exactly one provider-backed step from a known persisted state and assert the resulting parse, normalization, routing decision, and persisted state change
- logical-unit workflow tests that execute one logical unit across multiple invocations and assert repeat, ask-user, blocked, failed, or done behavior one step at a time
- full routed workflow tests that move across logical units, response submission, persistence, resume, and single-writer behavior

Across provider-backed tests, assertions should be made one invocation at a time so failures identify the exact step that regressed.

These tests should focus on:

- full routed runs from a starting unit through one or more routing decisions
- one-step assertions over provider requests, raw provider output, and resulting state changes
- repeated executions of the same active unit
- behavior across successive routed runs and response submission
- persisted state changes across successive invocations
- routing and state updates as observed through public workflow behavior

---

## Concrete Mock Provider

A single reusable mock provider should be defined and shared across provider-backed tests.

This provider should be the standard test double for routing step tests, logical-unit workflow tests, full routed workflow tests, and other tests in the solution that need to exercise the `ILlmProvider` boundary.

The standard test provider should implement `ILlmProvider` directly and should be treated as test infrastructure rather than as an ad hoc stub embedded in individual tests.

The standard test provider should be named `MockLlmProvider`.

By default the provider should:

- expose stable `Name`, `Description`, `DefaultModel`, and `SupportedModels` values
- report ready unless a test explicitly configures readiness failure
- run in strict mode so unexpected invocations fail the test immediately

The provider should accept an ordered invocation script.

The minimum shared test surface should include:

- a constructor or factory that accepts the ordered invocation script and an optional readiness error
- a requests collection that exposes the captured `CopilotRequest` values in call order
- a consumed invocation count or equivalent cursor that shows how much of the script ran
- an `AssertConsumed()` helper or equivalent assertion that the full script was used
- a per-invocation definition object that can hold raw output, thrown exception, expected prompt, expected model, expected source path, and a label

Each scripted invocation should be able to define:

- the raw string returned from `ExecuteAsync`
- an exception to throw instead of returning output
- optional expected `Prompt`, `Model`, and `SourcePath` values from the received `CopilotRequest`
- an optional label so test failures identify the exact step that failed

The provider should also:

- capture each received `CopilotRequest` in call order
- fail fast if `ExecuteAsync` is called more times than scripted
- fail fast if a scripted expectation does not match the received request
- expose the recorded requests and consumed call count for assertions
- expose a helper assertion that all scripted invocations were consumed
- never implement routing behavior itself; it only returns scripted provider output and records calls

The provider must be able to simulate:

- return fixed JSON payloads in sequence across successive calls
- simulate same-unit repeat results such as `[CONTINUE]`
- simulate explicit transitions such as `[SOME_ROUTING_KEYWORD]`
- simulate `[ASK_USER]`, `[ERROR]`, `[FAIL]`, and `[DONE]`
- simulate invalid keyword output
- simulate malformed JSON output
- simulate provider execution exceptions or cancellations
- simulate readiness failure
- inspect or assert the prompt passed into each step so tests can verify that the engine injected the correct normalized prompt input payload and rendered it correctly

---

## Required Step And Logical-Unit Scenarios

The following should be treated as required deterministic tests using the concrete mock provider:

- single-step success where exactly one provider call is executed and asserted in isolation
- prompt-rendering step where a single invocation verifies the normalized prompt input payload and rendered prompt
- same-unit repeat workflow where the active unit returns `[CONTINUE]` and remains active
- ask-user workflow where the engine applies normal successful state normalization, records returned questions, and stops
- done workflow where the session is marked complete and retains the last `activeUnitName`
- error workflow where the engine applies normal successful state normalization, sets the session to `blocked`, and alerts the user
- error-retry-with-response workflow where a blocked `[ERROR]` session receives extra operator context through `respond` before the next routed run
- fail workflow where the engine applies normal successful state normalization and execution stops as `failed`
- invalid-output workflow where malformed JSON or an invalid keyword causes immediate invocation failure without changing session state

---

## Required Routed Workflow Scenarios

The following should be treated as required routed workflow scenarios and should still be asserted one invocation at a time with the concrete mock provider:

- transition workflow where a keyword moves execution to another logical unit
- response-submission workflow where a user response is stored, the same unit resumes, and the response cursor advances after success
- state-replacement workflow where omitted or empty structured fields clear prior working state and only returned values persist for the next logical unit
- persistence-failure workflow where session state remains unchanged
- resume-failure workflow where persisted state references a missing logical unit or invalid schema
- single-writer workflow where overlapping writes to the same session are rejected

---

## Definition Validation Scenarios

The following should also be covered by tests without invoking the provider:

- duplicate allowed keywords in a unit are rejected
- duplicate transition keys in a unit are rejected
- a transition key not present in `allowedKeywords` is rejected
- a definition-specific keyword in `allowedKeywords` that is missing from `transitions` is rejected
- a built-in keyword appearing in `transitions` is rejected
- an invalid transition target logical unit name is rejected
- a unit cannot use a built-in keyword unless it appears in that unit's `allowedKeywords`

---

## Test Design Guidance

Each provider-backed test should define:

- whether the test is a single step, a logical-unit workflow, or a full routed workflow
- the routing definition under test
- the starting persisted state
- the ordered `MockLlmProvider` invocation script for each provider call, including returned raw output or thrown exception
- any user response provided between invocations
- the expected lifecycle status after each step
- the expected active unit after each step
- the expected last selected keyword after each step
- when relevant, the expected provider call count and confirmation that all scripted invocations were consumed
- when prompt assertions matter, the expected `definitionName`, `goal`, `status`, `lastSelectedKeyword`, `activeUnit`, `workingSummary`, `decisions`, `openQuestions`, `blockers`, and `pendingResponses` included in the normalized prompt input payload
- when relevant, the expected rejection of overlapping writes
- the expected persisted `workingSummary`, `decisions`, `openQuestions`, `blockers`, and stored response state, including fields intentionally cleared by omission or empty values
- the expected completion or stop condition

---

## Why This Is Separate

This document is separate so `routing.md` can stay focused on engine semantics rather than detailed testing guidance.
