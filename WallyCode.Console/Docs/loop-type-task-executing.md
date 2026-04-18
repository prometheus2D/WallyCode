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
- ask for clarification when needed
- `respond` provides clarification and resumes the loop immediately
- continue until more execution is not needed or the loop is done

---

## Suggested Units

- `review_task_queue`: assess pending work and decide whether to execute, ask for clarification, or stop
- `execute_task_batch`: complete a bounded batch of work
- `review_results`: validate outcomes and decide whether more execution is needed

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[MORE_EXECUTION_NEEDED]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`
