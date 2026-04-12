# WallyCode Loop Testing

This document defines the testing model for the loop routing system.

Related documents:

- `loop-routing.md`
- `loop-routing-examples.md`
- `loop-type-requirements-gathering.md`
- `loop-type-task-generating.md`
- `loop-type-task-executing.md`

---

## Purpose

This document focuses on workflow-level testing of the routing engine.

The goal is to verify that routed loop behavior is deterministic when provider output is controlled.

---

## Testing Scope

The required automated coverage is workflow-level coverage.

That means tests should focus on:

- full loop runs from a starting unit through one or more routing decisions
- repeated executions of the same active unit
- resume behavior across `loop` and `respond`
- persisted state changes across successive invocations
- routing, action execution, and normalization as observed through public workflow behavior

---

## Mock Provider Requirement

Workflow tests should use mock provider output to simulate each step.

The provider in tests should be able to:

- return fixed JSON payloads in sequence across successive calls
- simulate self-loop results such as `[CONTINUE]`
- simulate explicit transitions such as `[SOME_ROUTING_KEYWORD]`
- simulate `[ASK_USER]`, `[ERROR]`, `[FAIL]`, and `[DONE]`
- simulate invalid keyword output
- simulate malformed JSON output
- optionally inspect the prompt passed into each step so tests can verify that the workflow resumed with the correct state

---

## Required Workflow Scenarios

The following should be treated as required workflow scenarios:

- self-loop workflow where the active unit returns `[CONTINUE]` and remains active
- transition workflow where a keyword moves execution to another unit
- ask-user workflow where the engine records questions, enters `waiting-for-user`, and stops
- respond-and-resume workflow where the engine stops, `respond` stores a later user response, unread responses are injected on the next `loop` run, and the cursor advances only after that later loop iteration succeeds
- done workflow where the session is marked complete
- error workflow where blockers and summary persist and the user is alerted
- fail workflow where summary persists and execution stops
- invalid-output workflow where malformed JSON or an invalid keyword causes immediate failure
- persistence-failure workflow where canonical state remains unchanged
- action-failure workflow where canonical state remains unchanged
- resume-failure workflow where persisted state references a missing unit or invalid schema
- single-writer workflow where a second writer is rejected while the first writer is active

---

## Test Design Guidance

Each workflow test should define:

- the loop definition under test
- the starting persisted state
- the ordered mock provider outputs for each invocation
- any user responses appended between invocations
- the expected active unit after each step
- the expected phase after each step
- the expected persisted summary, decisions, questions, blockers, and response cursor
- the expected completion or stop condition

---

## Why This Is Separate

This document is separate so `loop-routing.md` can stay focused on engine semantics rather than detailed testing guidance.
