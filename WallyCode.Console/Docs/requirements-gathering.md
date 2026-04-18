# WallyCode Logical Units: Requirements Gathering

This document describes the logical units commonly used during requirements gathering.

Related documents:

- `routing.md`
- `routing-examples.md`
- `routing-testing.md`
- `task-generation.md`
- `task-execution.md`

---

## Purpose

Collect missing requirements from the user until the specification is clear enough for the next linked logical unit to move forward.

---

## Typical Behavior

- the active logical unit asks targeted questions
- `[ASK_USER]` stops the session when user input is needed
- the user answers with `respond`
- `respond` stores the response and resumes the same logical unit immediately
- when satisfied, it routes forward to the next linked logical unit or ends

---

## Suggested First Logical Unit

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

Suggested logical units:

- `collect_requirements`

This action is intentionally simple.

Its main special behavior is repeated stop-and-resume through built-in `[ASK_USER]` and `respond`.
