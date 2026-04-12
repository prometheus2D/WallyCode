# WallyCode Loop Routing Design

## Purpose

This document defines the future loop architecture.

A loop is the entry point into a JSON-defined flow of loop units.

In practice:

- the user chooses a loop
- that loop selects a starting loop unit
- the engine runs the active loop unit
- the active loop unit usually repeats on itself
- when the model returns a routing keyword that changes execution, the engine moves to another loop unit or ends

So a loop is not a separate runtime mechanism beyond routing.

A loop is the named starting point into a routed sequence of loop units.

This document describes that future state directly.

---

## Core Model

The runtime needs only a small number of concepts.

### 1. Loop

A loop is a named workflow entry point.

A loop definition declares:

- loop id
- name
- description
- start unit id
- shared instructions
- loop units

### 2. Loop Unit

A loop unit is the current mode of work.

Examples:

- collect missing requirements
- analyze requirements
- produce tasks
- review task queue
- execute task batch
- review results

Only one loop unit is active at a time.

### 3. Keyword

A keyword is the single machine-readable routing decision returned by the model.

Examples:

- `continue`
- `ask_user`
- `requirements_ready`
- `tasks_ready`
- `done`
- `error`
- `fail`

### 4. Transition

A transition is what happens when a keyword changes execution.

If no explicit transition exists for the selected keyword, execution stays on the current loop unit.

That means self-looping is the default behavior.

### 5. Action

An action is a small built-in engine behavior such as:

- wait for user input
- record summary
- record decisions
- record questions
- mark done

---

## Standard System Keywords

The future system should treat a small set of keywords as standard routing conventions across loops.

These can be hardcoded by the engine if that keeps authoring simpler and behavior more consistent.

### `continue`

This should be the standard default self-loop keyword.

Meaning:

- the current loop unit should continue
- no unit transition is needed
- the iteration succeeded
- state should still be updated normally

If `continue` has no explicit transition entry, the engine remains on the same loop unit.

### `error`

This should mean the model believes a technical or execution problem is preventing useful progress.

Examples:

- required file or artifact is missing
- expected input is malformed
- the current task cannot proceed because of a concrete technical issue

This is not the same as uncertainty.

This means the model believes there is a real blocking problem.

Recommended handling:

- record summary and blockers
- set phase to an error or blocked state
- stop the current invocation
- require operator inspection or a future recovery policy

### `fail`

This should mean the model cannot determine a valid next action inside the current loop unit.

Examples:

- the available keywords do not fit the situation
- the model cannot confidently choose the next action
- the prompt or loop definition is insufficient for the current state

This is not a technical execution error.

This is a routing failure or reasoning failure.

Recommended handling:

- record summary
- log the failure clearly
- stop the current invocation
- do not guess a transition

### Why these standard keywords are useful

They give every loop a small common control vocabulary:

- `continue` for normal self-looping
- `error` for concrete technical blockage
- `fail` for inability to determine the next valid action

That makes prompts, logs, and loop authoring more consistent.

---

## How Memory Works

Memory is not the routing system.

Memory is the persisted information the next iteration needs in order to continue work without losing context.

That means memory stores concrete things such as:

- the goal
- the selected loop id
- the active loop unit id
- the latest compact summary
- decisions already made
- questions already asked
- user responses already received
- which user responses have already been consumed by the model
- raw model outputs
- prompts sent to the provider
- logs

### What memory is for

Memory exists so the next iteration can answer questions like:

- what is the user trying to accomplish
- what has already been clarified
- what decisions are already fixed
- what questions are still open
- what did the model say last time
- which user responses are new and unread
- which loop unit should resume

### What memory is not for

Memory should not be the place where the engine guesses workflow behavior from large prose documents.

The engine should not need to infer things like:

- whether it is still collecting requirements
- whether it should move to task generation
- whether it should wait for the user
- whether it should finish

Those are routing decisions and should come from:

- active loop unit id
- selected keyword
- transition mapping
- explicit phase state

### Concrete example

Suppose the active loop unit is `collect_requirements`.

The persisted state might contain:

- goal: build a desktop app for invoice tracking
- active unit: `collect_requirements`
- working summary: target users are internal finance staff; export format still unclear
- decisions:
  - desktop first
  - local storage acceptable for v1
- open questions:
  - should exports be csv only or csv plus pdf
- last consumed user response id: 3

Then the user runs `respond` and adds:

- response 4: csv is enough for v1

On the next `loop` run, the engine does not replay every historical response as if it were new.

It does this instead:

1. load the active unit `collect_requirements`
2. load the stored summary, decisions, and open questions
3. load unread user responses where id > 3
4. inject only response 4 into the prompt
5. run the provider once
6. parse the returned keyword
7. either stay in `collect_requirements` or transition elsewhere
8. if successful, advance the consumed response cursor to 4

That is what memory does.

It preserves the exact persisted facts needed for the next iteration.

### Canonical memory should stay small

Canonical persisted memory should be limited to data that cannot be reconstructed cheaply from the loop definition or routing state.

Keep:

- goal
- loop id
- active loop unit id
- phase
- compact working summary
- decisions
- open questions
- append-only user responses
- consumed response cursor
- prompts
- raw outputs
- logs

Avoid making these canonical:

- rewritten workflow narratives every iteration
- repeated copies of unit instructions
- repeated copies of static loop guidance
- large markdown files that duplicate structured state

Human-readable files can still exist, but they should be generated artifacts, not the routing source of truth.

---

## Routing Rule

The routing rule should stay extremely simple:

**Run the active loop unit again unless the selected keyword explicitly transitions to another loop unit, waits for user input, or ends the loop.**

This is the default behavior of the engine.

In normal operation, `continue` should be the standard keyword used for this self-loop case.

---

## State Normalization After Every Successful Iteration

This needs to be explicit.

After every successful iteration, the engine should normalize persisted state before ending the invocation.

This applies in both cases:

- the loop stays on the same loop unit
- the loop transitions to another loop unit

The only difference between those two outcomes is whether the active loop unit id changes.

Everything else should follow the same cleanup and persistence pipeline.

### The engine should always do these things after a successful iteration

1. accept the selected keyword
2. resolve the matching transition if one exists
3. execute any built-in actions
4. normalize persisted loop state
5. set the next active loop unit id
6. persist the updated loop execution state
7. persist logs, prompt, and raw output
8. end the current invocation

### Data that should carry forward after a successful iteration

These values should normally persist because the next iteration still needs them:

- goal
- selected loop id
- append-only user response history
- last consumed user response id
- last consumed user response timestamp
- durable decisions that are still valid
- compact working summary, updated to the newest useful version
- logs, prompts, and raw outputs

### Data that should be overwritten after a successful iteration

These values describe the latest routing result and should be replaced each time:

- active loop unit id
- last selected keyword
- last routing outcome
- phase
- working summary when a newer summary is returned

### Data that should be cleared or filtered after a successful iteration

These values should not be blindly carried forward:

- open questions that were resolved in the completed iteration
- temporary unit-local notes if such notes exist
- stale blockers that no longer apply

The rule is simple:

- keep unresolved facts
- remove resolved temporary state
- do not carry stale local clutter into the next iteration

### Response cursor behavior after a successful iteration

The consumed user response cursor should not reset.

If response 7 was the last consumed response before or during the iteration, the next iteration should continue from that same cursor.

Otherwise the engine would re-read old user input as if it were new.

### Self-loop example

Suppose the active loop unit is `collect_requirements` and the model returns:

- selected keyword: `continue`
- summary: one requirement is still unclear
- decisions:
  - desktop first
- open questions:
  - should exports be csv only or csv plus pdf

There is no explicit transition, so the loop stays on `collect_requirements`.

After persistence, the state should look conceptually like this:

- active loop unit = `collect_requirements`
- phase = `active`
- working summary = one requirement is still unclear
- decisions = desktop first
- open questions = should exports be csv only or csv plus pdf
- last consumed user response id = updated only if unread responses were consumed successfully

That means the same loop unit continues, but stale state is still cleaned up and replaced with the newest useful state.

### Transition example

Suppose the active loop unit is `collect_requirements` and the model returns:

- selected keyword: `requirements_ready`
- summary: requirements are now clear enough to produce tasks
- decisions:
  - desktop first
  - csv export only for v1
- open questions: none

The transition moves to `produce_tasks`.

After persistence, the state should look conceptually like this:

- active loop unit = `produce_tasks`
- phase = `active`
- working summary = requirements are now clear enough to produce tasks
- decisions = desktop first, csv export only for v1
- open questions = empty
- last consumed user response id = unchanged except for any newly consumed responses in the completed iteration

That means the next loop unit starts with the clarified facts, not with stale unresolved-question state from the previous loop unit.

### Default normalization policy

Unless a loop definition says otherwise, the engine should follow this default policy:

- preserve durable context
- overwrite routing fields
- filter resolved questions and blockers
- keep the response cursor intact
- never fully reset loop state during normal iteration flow

---

## Future JSON Structure

The future JSON structure should be explicit enough to author real workflows without adding workflow-specific C#.

### Recommended top-level shape

```json
{
  "id": "requirements-collection",
  "name": "Requirements Collection",
  "description": "Collect missing requirements from the user until the specification is clear enough to move forward.",
  "startUnit": "collect_requirements",
  "sharedInstructions": "Stay concrete. Ask the fewest questions that unlock the most progress.",
  "units": [
    {
      "id": "collect_requirements",
      "title": "Collect Requirements",
      "purpose": "Identify the highest-value missing requirement or decision and either ask the user or conclude that requirements are ready.",
      "instructions": "Review the goal, stored summary, decisions, open questions, and unread user responses. Choose exactly one keyword.",
      "allowedKeywords": [
        "continue",
        "ask_user",
        "requirements_ready",
        "error",
        "fail",
        "done"
      ],
      "transitions": [
        {
          "keyword": "ask_user",
          "actions": ["wait-for-user", "record-question"]
        },
        {
          "keyword": "requirements_ready",
          "nextUnit": "done",
          "actions": ["record-summary", "mark-done"]
        },
        {
          "keyword": "error",
          "actions": ["record-summary", "set-phase:error"]
        },
        {
          "keyword": "fail",
          "actions": ["record-summary", "set-phase:failed"]
        },
        {
          "keyword": "done",
          "nextUnit": "done",
          "actions": ["mark-done"]
        }
      ]
    }
  ]
}
```

### Top-level fields

#### `id`
Stable machine-readable loop id.

#### `name`
Human-readable loop name.

#### `description`
Short explanation of what the loop is for.

#### `startUnit`
The loop unit where execution begins.

#### `sharedInstructions`
Instructions included in every prompt for this loop.

#### `units`
The list of loop units available to the loop.

---

## Loop Unit Structure

Each loop unit should support the following fields.

```json
{
  "id": "produce_tasks",
  "title": "Produce Tasks",
  "purpose": "Turn clarified requirements into an ordered task list.",
  "instructions": "Produce or refine the task list. Choose exactly one keyword.",
  "allowedKeywords": [
    "continue",
    "ask_user",
    "tasks_ready",
    "error",
    "fail",
    "done"
  ],
  "transitions": [
    {
      "keyword": "ask_user",
      "actions": ["wait-for-user", "record-question"]
    },
    {
      "keyword": "tasks_ready",
      "nextUnit": "done",
      "actions": ["record-summary", "mark-done"]
    },
    {
      "keyword": "error",
      "actions": ["record-summary", "set-phase:error"]
    },
    {
      "keyword": "fail",
      "actions": ["record-summary", "set-phase:failed"]
    },
    {
      "keyword": "done",
      "nextUnit": "done",
      "actions": ["mark-done"]
    }
  ]
}
```

### Loop unit fields

#### `id`
Stable machine-readable unit id.

#### `title`
Human-readable unit title.

#### `purpose`
What this unit is trying to accomplish.

#### `instructions`
Prompt instructions specific to this unit.

#### `allowedKeywords`
The exact keywords the model is allowed to return while this unit is active.

#### `transitions`
Optional explicit routing rules.

If a selected keyword has no explicit transition entry, the engine stays on the same loop unit.

That implicit self-loop behavior is part of the design.

In normal cases, `continue` should be the keyword used for that behavior.

---

## Transition Structure

A transition only needs to exist when a keyword changes execution behavior or should trigger actions.

```json
{
  "keyword": "tasks_ready",
  "nextUnit": "execute_tasks",
  "actions": ["record-summary"]
}
```

### Transition fields

#### `keyword`
The keyword that triggers this transition.

#### `nextUnit`
Optional target unit.

Allowed values:

- another unit id
- `done`
- omitted, which means remain on the current unit

#### `actions`
Optional built-in actions to execute after the keyword is accepted.

---

## Output Contract

Each iteration must return strict JSON.

```json
{
  "status": "continue",
  "selectedKeyword": "ask_user",
  "summary": "One export-format question remains before requirements are complete.",
  "workLog": "Reviewed unread user responses and narrowed the remaining ambiguity to export format.",
  "questions": [
    "Should exports support csv only, or csv plus pdf?"
  ],
  "decisions": [
    "Desktop first",
    "Local storage is acceptable for v1"
  ],
  "assumptions": [],
  "blockers": [],
  "doneReason": ""
}
```

### Required behavior

- exactly one `selectedKeyword`
- keyword must match the active loop unit's `allowedKeywords`
- output must be valid JSON
- invalid keyword means immediate failure
- no guessing
- no repair prompt in v1

### Standard keyword behavior

#### `continue`
- successful iteration
- remain on current loop unit unless an explicit transition says otherwise
- still run normal state normalization after the iteration
- persist updated summary, decisions, questions, blockers, and response cursor normally

#### `error`
- successful structured output indicating a technical blockage
- persist summary and blockers
- move phase into an error or blocked state
- stop current invocation

#### `fail`
- successful structured output indicating the model cannot determine a valid next action
- persist summary
- move phase into a failed state
- stop current invocation

---

## Prompt Contents

The prompt for each iteration should include:

- shared loop instructions
- active loop unit id and title
- active loop unit purpose
- active loop unit instructions
- allowed keywords
- explicit instruction to choose exactly one keyword
- goal
- compact working summary
- decisions already recorded
- open questions already recorded
- unread user responses only

The prompt should not include large derived workflow documents unless they provide unique value that is not already present in structured state.

---

## User Response Flow

The requirements collection loop must support explicit user interaction through `respond`.

### Required behavior

1. active loop unit selects `ask_user`
2. engine records returned questions
3. engine executes `wait-for-user`
4. phase becomes `waiting-for-user`
5. current invocation ends
6. user runs `respond`
7. response is appended to the response store
8. next `loop` run resumes the same active loop unit unless a transition changed it
9. unread responses are injected into the prompt
10. after a successful iteration, the consumed response cursor advances

This is how a loop unit requests user input and then picks back up exactly where it left off.

---

## First Three Loops

The first three loop definitions should be:

### 1. Requirements Collection

Purpose:

- collect missing requirements from the user until the specification is clear enough to move forward

Typical behavior:

- active loop unit asks targeted questions
- user answers with `respond`
- same loop unit resumes
- when satisfied, it routes forward or ends

Suggested starting unit:

- `collect_requirements`

Suggested keywords:

- `continue`
- `ask_user`
- `requirements_ready`
- `error`
- `fail`
- `done`

### 2. Requirements To Tasks

Purpose:

- turn a requirement or definition document into a concrete ordered task list

Suggested units:

- `analyze_requirements`
- `produce_tasks`

Suggested keywords:

- `continue`
- `ask_user`
- `tasks_ready`
- `error`
- `fail`
- `done`

### 3. Execute Tasks

Purpose:

- execute tasks until completion

Suggested units:

- `review_task_queue`
- `execute_task_batch`
- `review_results`

Suggested keywords:

- `continue`
- `need_clarification`
- `more_execution_needed`
- `error`
- `fail`
- `done`

These loops are not different runtime systems.

They are different starting points into different sets of loop units.

---

## Required Persisted State

### Session state

Session state should hold:

- session identity
- selected loop id
- lifecycle status
- completion status
- timestamps

### Loop execution state

Loop execution state should hold:

- active loop unit id
- phase
- last selected keyword
- last routing outcome
- compact working summary
- decisions
- open questions
- last consumed user response id
- last consumed user response timestamp

That is enough to resume deterministically.

---

## Built-In Actions for V1

Recommended built-in actions:

### `wait-for-user`
- set phase to `waiting-for-user`
- end current invocation after persistence

### `mark-done`
- set phase to `done`
- mark session complete

### `set-phase:<value>`
- set explicit phase value

### `record-summary`
- persist compact summary

### `record-decision`
- persist decisions

### `record-question`
- persist questions

Not included in v1:

- arbitrary shell commands
- arbitrary file edits from JSON
- arbitrary provider chaining from JSON

---

## Testing Model

The future architecture should include a dedicated unit test project for routed loops.

The purpose is to test loop behavior deterministically without depending on a real external provider.

### Test provider concept

The test project should include a `TestProvider` implementation that stands in for the real provider.

That provider should be able to return controlled outputs for test scenarios such as:

- `continue` on the same loop unit
- transition to another loop unit
- `ask_user` and wait-for-user behavior
- `error`
- `fail`
- `done`
- invalid keyword output
- malformed JSON output

### Why this matters

The routed loop engine is mostly deterministic once provider output is fixed.

That means the most valuable tests are not “real AI” tests.

They are programmatic routing tests that verify:

- prompt input is built from the correct persisted state
- unread user responses are consumed correctly
- the selected keyword is validated correctly
- self-loop behavior works correctly
- transition behavior works correctly
- state normalization works correctly
- response cursor behavior works correctly
- wait-for-user behavior works correctly
- error and fail behavior work correctly
- invalid output fails fast

### Controlled provider behavior

The test provider does not need to imitate intelligence.

It needs to imitate provider responses in a controlled way.

That can include:

- returning fixed JSON payloads
- returning different payloads across successive calls
- using simple lambdas or scripted callbacks to inspect prompt text
- optionally performing deterministic test-side file actions when a scenario needs to emulate external effects

The important point is not the exact implementation shape.

The important point is that routed loops must be testable without a live provider.

### Testing scope

The future system should be designed so loop definitions and routing behavior can be validated through automated tests.

That includes tests for:

- loop definition loading and validation
- routing keyword validation
- implicit self-loop behavior
- explicit transitions
- built-in actions
- response cursor advancement
- persisted state normalization
- resume behavior across multiple invocations

This testing model should be treated as part of the future architecture, not as an optional afterthought.

---

## Logging and Observability

Each iteration log should include:

- loop id
- active loop unit id
- selected keyword
- whether execution stayed or transitioned
- actions executed
- next active loop unit id
- phase after routing
- summary
- done reason if any

---

## Safety Limits

Even with `continue` as the standard self-loop keyword, the runtime should still enforce practical safety limits.

Examples:

- max iterations per invocation
- max repeated self-loops without meaningful state change
- operator-visible warnings when the same unit keeps returning low-value `continue` results

These limits are runtime safeguards.

They do not change the routing model.

---

## Final Direction

The future architecture should optimize for one simple rule:

**A loop is a named starting point into a set of loop units. One loop unit is active at a time. That loop unit repeats by default. The model returns one keyword. That keyword either keeps execution on the same unit, waits for user input, moves to another unit, or ends the loop.**

That gives the system:

- explicit routing
- small persisted state
- deterministic resume behavior
- clean `respond` integration
- lower prompt cost
- simpler loop authoring
- less engine complexity
- deterministic automated testing without a live provider
