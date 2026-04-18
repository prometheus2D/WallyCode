# WallyCode Routing Examples

This document holds concrete logical-unit routing examples that were split out of `routing.md` so the core routing document can stay focused on generic engine behavior.

Related documents:

- `routing.md`
- `routing-testing.md`
- `requirements-gathering.md`
- `task-generation.md`
- `task-execution.md`

---

## Requirements Collection Example

These examples show linked logical units inside one routed definition.

Example active unit:

- `collect_requirements`

Example transition target:

- `produce_tasks`

Example repeat result:

- selected keyword: `[CONTINUE]`
- remain on `collect_requirements`
- session status: `active`

Example transition result:

- selected keyword: `[REQUIREMENTS_READY]`
- move from logical unit `collect_requirements` to logical unit `produce_tasks`
- stay inside the same routed definition
- session status: `active`

Example ask-user result:

- selected keyword: `[ASK_USER]`
- remain on `collect_requirements`
- stop the current session
- session status: `blocked`
- user answers with `respond`
- `respond` appends the response and immediately runs the active logical unit again on `collect_requirements`

Example error result:

- selected keyword: `[ERROR]`
- remain on `collect_requirements`
- apply normal successful state normalization
- stop the current session
- session status: `blocked`

Example fail result:

- selected keyword: `[FAIL]`
- remain on `collect_requirements`
- apply normal successful state normalization
- stop the current session
- session status: `failed`

---

## Generic Mapping Reminder

These examples are only illustrations.

The routing engine itself does not care whether a logical unit is named:

- `collect_requirements`
- `produce_tasks`
- `review_results`
- `example_unit`

The engine only cares about:

- active unit name
- selected keyword
- built-in keyword behavior
- matching transition for definition-specific keywords
- definition transition target unit name when a definition-specific keyword transitions
