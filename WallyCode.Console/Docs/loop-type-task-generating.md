# WallyCode Loop Type: Task Generating

This document describes the task-generating loop type.

Related documents:

- `loop-routing.md`
- `loop-routing-examples.md`
- `loop-testing.md`
- `loop-type-requirements-gathering.md`
- `loop-type-task-executing.md`

---

## Purpose

Turn a requirement or definition document into a concrete ordered task list.

---

## Typical Behavior

- analyze the current requirement state
- produce or refine a task list
- use built-in `[ASK_USER]` only when user clarification is needed
- normal `respond` can provide that clarification and trigger the loop to continue
- store-only `respond` mode may be used when extra user input should be recorded before a later loop run
- route forward when the task list is ready

---

## Suggested Units

- `analyze_requirements`
- `produce_tasks`

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[TASKS_READY]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`
