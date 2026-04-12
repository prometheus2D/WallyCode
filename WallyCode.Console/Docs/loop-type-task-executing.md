# WallyCode Loop Type: Task Executing

This document describes the task-executing loop type.

Related documents:

- `loop-routing.md`
- `loop-routing-examples.md`
- `loop-testing.md`
- `loop-type-requirements-gathering.md`
- `loop-type-task-generating.md`

---

## Purpose

Execute tasks until completion.

---

## Typical Behavior

- review the task queue
- execute a batch of work
- review results
- continue until more execution is not needed or the loop is done

---

## Suggested Units

- `review_task_queue`
- `execute_task_batch`
- `review_results`

---

## Suggested Keywords

- `[CONTINUE]`
- `[NEED_CLARIFICATION]`
- `[MORE_EXECUTION_NEEDED]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`
