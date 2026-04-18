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

This document focuses on workflow-level testing of the routing engine.

The goal is to verify that routed execution behavior is deterministic when provider output is controlled.

These tests verify the contracts defined in `routing.md`.

---

## Testing Scope

The required automated coverage is workflow-level coverage.

That means tests should focus on:

- full routed runs from a starting unit through one or more routing decisions
- repeated executions of the same active unit
- behavior across successive routed runs and response submission
- persisted state changes across successive invocations
- routing and state updates as observed through public workflow behavior

---

## Mock Provider Requirement

Workflow tests should use mock provider output to simulate each step.

The provider in tests should be able to:

- return fixed JSON payloads in sequence across successive calls
- simulate same-unit repeat results such as `[CONTINUE]`
- simulate explicit transitions such as `[SOME_ROUTING_KEYWORD]`
- simulate `[ASK_USER]`, `[ERROR]`, `[FAIL]`, and `[DONE]`
- simulate invalid keyword output
- simulate malformed JSON output
- optionally inspect the prompt passed into each step so tests can verify that the engine injected the correct normalized prompt input payload and rendered it correctly

---

## Required Workflow Scenarios

The following should be treated as required workflow scenarios:

- same-unit repeat workflow where the active unit returns `[CONTINUE]` and remains active
- transition workflow where a keyword moves execution to another logical unit
- ask-user workflow where the engine applies normal successful state normalization, records returned questions, and stops
- response-submission workflow where a user response is stored, the same unit resumes, and the response cursor advances after success
- done workflow where the session is marked complete and retains the last `activeUnitName`
- error workflow where the engine applies normal successful state normalization, sets the session to `blocked`, and alerts the user
- fail workflow where the engine applies normal successful state normalization and execution stops as `failed`
- state-replacement workflow where omitted or empty structured fields clear prior working state and only returned values persist for the next logical unit
- invalid-output workflow where malformed JSON or an invalid keyword causes immediate invocation failure without changing canonical state
- persistence-failure workflow where canonical state remains unchanged
- resume-failure workflow where persisted state references a missing logical unit or invalid schema
- single-writer workflow where a second writer is rejected while the first writer is active
- stale-lock recovery workflow where an expired lock is safely replaced by a new writer

---

## Definition Validation Scenarios

The following should also be covered by tests:

- duplicate allowed keywords in a unit are rejected
- duplicate transition keys in a unit are rejected
- a transition key not present in `allowedKeywords` is rejected
- a definition-specific keyword in `allowedKeywords` that is missing from `transitions` is rejected
- a built-in keyword appearing in `transitions` is rejected
- an invalid transition target logical unit name is rejected
- a unit cannot use a built-in keyword unless it appears in that unit's `allowedKeywords`

---

## Test Design Guidance

Each workflow test should define:

- the routing definition under test
- the starting persisted state
- the ordered mock provider outputs for each invocation
- any user response provided between invocations
- the expected lifecycle status after each step
- the expected active unit after each step
- the expected last selected keyword after each step
- when prompt assertions matter, the expected `definitionName`, `goal`, `status`, `lastSelectedKeyword`, `activeUnit`, `workingSummary`, `decisions`, `openQuestions`, `blockers`, and `pendingResponses` included in the normalized prompt input payload
- when relevant, the expected lock acquisition or stale-lock takeover result
- the expected persisted `workingSummary`, `decisions`, `openQuestions`, `blockers`, and stored response state, including fields intentionally cleared by omission or empty values
- the expected completion or stop condition

---

## Why This Is Separate

This document is separate so `routing.md` can stay focused on engine semantics rather than detailed testing guidance.
