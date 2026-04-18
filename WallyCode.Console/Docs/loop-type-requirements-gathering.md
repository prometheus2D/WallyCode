# WallyCode Loop Type: Requirements Gathering

This document describes the requirements-gathering loop type.

Related documents:

- `loop-routing.md`
- `loop-routing-examples.md`
- `loop-testing.md`
- `loop-type-task-generating.md`
- `loop-type-task-executing.md`

---

## Purpose

Collect missing requirements from the user until the specification is clear enough to move forward.

---

## Typical Behavior

- the active loop unit asks targeted questions
- `[ASK_USER]` stops the loop when user input is needed
- the user answers with `respond`
- `respond` stores the response and resumes the same unit immediately
- when satisfied, it routes forward or ends

---

## Suggested Starting Unit

- `collect_requirements`

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[REQUIREMENTS_READY]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`

---

## Suggested Unit Shape

Suggested units:

- `collect_requirements`

This loop type is intentionally simple.

Its main special behavior is repeated stop-and-resume through built-in `[ASK_USER]` and `respond`.
