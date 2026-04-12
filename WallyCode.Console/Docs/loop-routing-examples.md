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

Example transition result:

- selected keyword: `[REQUIREMENTS_READY]`
- move from `collect_requirements` to `produce_tasks`

Example ask-user result:

- selected keyword: `[ASK_USER]`
- remain on `collect_requirements`
- execute `wait-for-user`
- stop and wait for `respond`

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
- matching transition
- optional `nextUnit`
- optional `wait-for-user`
