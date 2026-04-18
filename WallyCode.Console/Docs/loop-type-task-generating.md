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
- `respond` provides that clarification and resumes the loop immediately
- route forward when the task list is ready

---

## Suggested Units

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
