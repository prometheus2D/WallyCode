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
- produce or refine a task list
- use built-in `[ASK_USER]` only when user clarification is needed
- `respond` provides that clarification and resumes the same logical unit immediately
- route forward when the task list is ready for the next linked logical unit

---

## Suggested Logical Units

These are linked logical units inside the same routed definition.

- `analyze_requirements`: assess the current requirement state and decide whether more clarification is needed before task generation
- `produce_tasks`: generate or refine the concrete ordered task list and route forward when it is ready

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[TASKS_READY]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`
