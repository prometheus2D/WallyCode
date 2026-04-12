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
- the user answers later with `respond`
- the same loop unit resumes on the next `loop` run
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

Its main special behavior is repeated pause-and-resume through `[ASK_USER]` and `respond`.
