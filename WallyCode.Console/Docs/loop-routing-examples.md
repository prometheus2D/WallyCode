# WallyCode Loop Routing Examples

This document holds loop-specific and concrete routing examples that were split out of `loop-routing.md` so the core routing document can stay focused on generic engine behavior.

Related documents:

- `loop-routing.md`
- `loop-testing.md`
- `loop-type-requirements-gathering.md`
- `loop-type-task-generating.md`
- `loop-type-task-executing.md`

---

## Requirements Collection Example

Example active unit:

- `collect_requirements`

Example transition target:

- `produce_tasks`

Example self-loop result:

- selected keyword: `[CONTINUE]`
- remain on `collect_requirements`
- session status: `active`
- last routing outcome: `self-loop`

Example transition result:

- selected keyword: `[REQUIREMENTS_READY]`
- move from `collect_requirements` to `produce_tasks`
- session status: `active`
- last routing outcome: `transition`

Example ask-user result:

- selected keyword: `[ASK_USER]`
- remain on `collect_requirements`
- stop the loop
- session status: `blocked`
- last routing outcome: `ask-user`
- user answers with `respond`
- in normal mode, `respond` triggers the loop to run again on `collect_requirements`

Example store-only respond result:

- user runs `respond` in store-only mode
- response text is recorded
- later store-only responses add more response text instead of replacing prior text
- the loop does not start yet
- a later `loop` run uses the stored response context

---

## Generic Mapping Reminder

These examples are only illustrations.

The routing engine itself does not care whether a unit is named:

- `collect_requirements`
- `produce_tasks`
- `review_results`
- `example_unit`

The engine only cares about:

- active unit id
- selected keyword
- built-in keyword behavior
- matching transition for loop-specific keywords
- definition `destinationUnitId` when a loop-specific keyword transitions
