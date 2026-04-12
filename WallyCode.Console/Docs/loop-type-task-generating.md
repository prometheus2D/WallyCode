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
- ask the user for clarification only when needed
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
