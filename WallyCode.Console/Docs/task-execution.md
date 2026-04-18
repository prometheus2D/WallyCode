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
- execute one bounded batch of work
- review the results of that batch
- ask for clarification when needed
- `respond` provides clarification and resumes the same logical unit immediately
- if the logical unit returns `[ERROR]`, default recovery is to fix the execution problem and start another routed run; use `respond` first only when extra operator context is needed before that retry
- continue until more execution is not needed or the session is complete

---

## Suggested Logical Units

This logical unit keeps task execution aligned with the routing model's same-unit repeat behavior.

- `execute_tasks`: review the queue, complete one bounded batch of work, review the result, and decide whether to continue, ask for clarification, or stop

---

## Suggested Keywords

- `[CONTINUE]`
- `[ASK_USER]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`

Use `[CONTINUE]` to keep `execute_tasks` active while more bounded execution is still needed.
