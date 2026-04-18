# WallyCode Logical Units: Task Execution

This document describes the logical units commonly used during task execution.

Related documents:

- `routing.md`
- `routing-examples.md`
- `routing-testing.md`
- `requirements-gathering.md`
- `task-generation.md`

---

## Purpose

Execute tasks until completion.

---

## Typical Behavior

- review the task queue
- execute a batch of work
- review results
- ask for clarification when needed
- `respond` provides clarification and resumes the same logical unit immediately
- continue until more execution is not needed or the session is complete

---

## Suggested Logical Units

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
