# WallyCode Logical Units: Task Generation

This document describes the logical units commonly used during task generation.

Related documents:

- `routing.md`
- `routing-examples.md`
- `routing-testing.md`
- `requirements-gathering.md`
- `task-execution.md`

---

## Purpose

Turn the current requirement state into a concrete ordered task list.

---

## Typical Behavior

- analyze the current requirement state
- use built-in `[ASK_USER]` only when user clarification is needed
- `respond` provides that clarification and resumes the same logical unit immediately
- if the logical unit returns `[ERROR]`, default recovery is to fix the execution problem and start another routed run; use `respond` first only when extra operator context is needed before that retry
- produce or refine a task list
- route forward when the task list is ready for the next linked logical unit

---

## Suggested Logical Units

This logical unit keeps task generation aligned with the routing model's same-unit repeat behavior.

- `produce_tasks`: assess whether the current requirement state is sufficient, ask for clarification when it is not, and generate or refine the concrete ordered task list when it is

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[TASKS_READY]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`

Use `[CONTINUE]` to keep `produce_tasks` active while the task list is still being refined.

Use `[TASKS_READY]` to route from `produce_tasks` to the next linked logical unit.
