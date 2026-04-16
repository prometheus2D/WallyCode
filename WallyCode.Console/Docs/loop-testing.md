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
- behavior across `loop` and `respond`
- persisted state changes across successive invocations
- routing and state updates as observed through public workflow behavior

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
- ask-user workflow where the engine records questions and stops
- normal-mode respond workflow where `respond` stores a user response and triggers the loop to run again
- store-only respond workflow where `respond` stores a user response without immediately running the loop
- additive store-only workflow where repeated store-only responses add to stored response context instead of replacing prior text
- respond-and-continue workflow where the resumed run includes the stored response and then advances the response cursor after success
- done workflow where the session is marked complete and retains the last `activeUnitId`
- error workflow where blockers and summary persist and the user is alerted
- fail workflow where summary persists and execution stops
- invalid-output workflow where malformed JSON or an invalid keyword causes immediate failure
- persistence-failure workflow where canonical state remains unchanged
- resume-failure workflow where persisted state references a missing unit or invalid schema
- single-writer workflow where a second writer is rejected while the first writer is active

---

## Definition Validation Scenarios

The following should also be covered by tests:

- duplicate allowed keywords in a unit are rejected
- duplicate transition keywords in a unit are rejected
- a transition keyword not present in `allowedKeywords` is rejected
- a built-in keyword appearing in `transitions` is rejected
- an invalid `nextUnit` target is rejected
- a unit cannot use a built-in keyword unless it appears in that unit's `allowedKeywords`

---

## Test Design Guidance

Each workflow test should define:

- the loop definition under test
- the starting persisted state
- the ordered mock provider outputs for each invocation
- any user response provided between invocations
- whether `respond` is normal mode or store-only mode
- the expected lifecycle status after each step
- the expected active unit after each step
- the expected last routing outcome after each step
- the expected persisted summary, decisions, questions, blockers, and stored response state
- the expected completion or stop condition

---

## Why This Is Separate

This document is separate so `loop-routing.md` can stay focused on engine semantics rather than detailed testing guidance.
