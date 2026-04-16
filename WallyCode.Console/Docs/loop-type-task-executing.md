# WallyCode Loop Type: Task Executing

This document describes the task-executing loop type.

Related documents:

- `loop-routing.md`
- `loop-routing-examples.md`
- `loop-testing.md`
- `loop-type-requirements-gathering.md`
- `loop-type-task-generating.md`

---

## Status Note

This document describes the target routed behavior for this loop type.

Current runtime note:

- the runtime still uses one generic iterative loop rather than routed loop units
- `respond` stores response text only
- the operator runs `loop` again to continue

## Purpose

Execute tasks until completion.

---

## Typical Behavior

- review the task queue
- execute a batch of work
- review results
- ask for clarification when needed
- in the routed design, normal `respond` can provide clarification and trigger the loop to continue
- store-only `respond` mode may be used when extra user input should be recorded before a later loop run
- continue until more execution is not needed or the loop is done

---

## Suggested Units

- `review_task_queue`
- `execute_task_batch`
- `review_results`

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[MORE_EXECUTION_NEEDED]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`
