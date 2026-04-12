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

Keywords should use a visually explicit format:

- uppercase
- surrounded by square brackets

Examples:

- `[CONTINUE]`
- `[ASK_USER]`
- `[REQUIREMENTS_READY]`
- `[TASKS_READY]`
- `[DONE]`
- `[ERROR]`
- `[FAIL]`

This makes routing decisions easier to see in prompts, logs, and raw outputs.

### 4. Transition

A transition is what happens when a keyword changes execution.

If no explicit transition exists for the selected keyword, execution stays on the current loop unit.

That means self-looping is the default behavior.

### 5. Action

For v1, actions should stay minimal.

The only built-in action needed in v1 is:

- wait for user input

The engine should not use actions as a mini command language for normal persistence or routing state updates.

---

## Standard System Keywords

The future system should treat a small set of keywords as standard routing conventions across loops.

These can be hardcoded by the engine if that keeps authoring simpler and behavior more consistent.

### `[CONTINUE]`

This should be the standard default self-loop keyword.

Meaning:

- the current loop unit should continue
- no unit transition is needed
- the iteration succeeded
- state should still be updated normally

If `[CONTINUE]` has no explicit transition entry, the engine remains on the same loop unit.

### `[ERROR]`

This should mean a concrete action failed and the user must be alerted.

Examples:

- a command failed
- a PowerShell script failed
- a file operation failed
- a literal programming action failed

This is not the same as uncertainty.

This means the loop encountered a real execution problem.

Required handling:

- persist summary and blockers
- persist all related debugging information
- alert the user clearly
- stop the current process

### `[FAIL]`

This should mean the model cannot determine a valid next action inside the current loop unit.

Examples:

- the available keywords do not fit the situation
- the model cannot confidently choose the next action
- the prompt or loop definition is insufficient for the current state

This is not a technical execution error.

This is a routing failure or reasoning failure.

Recommended handling:

- persist summary
- log the failure clearly
- stop the current invocation
- do not guess a transition

### `[DONE]`

This should mean the loop is fully finished.

`[DONE]` is the completion signal.

The engine should not require an extra completion action beyond accepting `[DONE]`.

### Why these standard keywords are useful

They give every loop a small common control vocabulary:

- `[CONTINUE]` for normal self-looping
- `[ERROR]` for concrete execution blockage
- `[FAIL]` for inability to determine the next valid action
- `[DONE]` for explicit completion

That makes prompts, logs, and loop authoring more consistent.

---

## Canonical Runtime Values

The runtime should use a small fixed vocabulary for persisted state so resume behavior stays deterministic.

### Session status

Recommended values:

- `active`
- `completed`
- `failed`
- `blocked`

This is the overall session status.

### Loop phase

Recommended values:

- `active`
- `waiting-for-user`
- `done`
- `error`
- `failed`

This is the current loop execution phase.

### Routing outcome

Recommended values:

- `self-loop`
- `transition`
- `waiting-for-user`
- `done`
- `error`
- `fail`
- `invalid-output`

This is the normalized result of the last completed invocation.

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

## Canonical Persisted State Shape

The persisted state should have an explicit schema rather than only a conceptual field list.

### Session state shape

```json
{
  "schemaVersion": 1,
  "sessionId": "session-001",
  "loopId": "requirements-collection",
  "status": "active",
  "isComplete": false,
  "createdAtUtc": "2025-01-01T00:00:00Z",
  "updatedAtUtc": "2025-01-01T00:00:00Z"
}
```

### Loop execution state shape

```json
{
  "schemaVersion": 1,
  "activeUnitId": "collect_requirements",
  "phase": "active",
  "lastSelectedKeyword": "[CONTINUE]",
  "lastRoutingOutcome": "self-loop",
  "workingSummary": "One requirement is still unclear.",
  "decisions": [
    "Desktop first"
  ],
  "openQuestions": [
    "Should exports support csv only, or csv plus pdf?"
  ],
  "blockers": [],
  "lastConsumedUserResponseId": 4,
  "lastConsumedUserResponseTimestampUtc": "2025-01-01T00:00:00Z"
}
```

### Schema rules

- persisted state must include `schemaVersion`
- persisted state must be forward-migratable
- missing required fields in persisted state should cause deterministic load failure
- the engine should not silently invent missing routing fields during resume

---

## Routing Rule

The routing rule should stay extremely simple:

**Run the active loop unit again unless the selected keyword explicitly transitions to another loop unit, waits for user input, or ends the loop.**

This is the default behavior of the engine.

In normal operation, `[CONTINUE]` should be the standard keyword used for this self-loop case.

---

## Atomic Iteration Semantics

The engine should treat one loop invocation as a single logical transaction.

### Required rule

Routing resolution, action execution, state normalization, cursor advancement, and persistence should succeed or fail as one atomic update.

### Practical meaning

If any step after provider output parsing fails, the engine should not partially commit:

- next active unit changes
- phase changes
- summary changes
- decisions changes
- questions changes
- blockers changes
- consumed response cursor changes

### Raw artifacts on failure

Even when the iteration fails, the engine should still persist diagnostic artifacts such as:

- raw provider output
- prompt text
- parse error details
- exception details
- invocation log entry

Those diagnostic writes should be clearly separated from canonical loop execution state.

### Action failure rule

If a built-in action fails:

- the invocation fails
- canonical loop execution state remains unchanged
- consumed response cursor does not advance
- failure details are logged and persisted
- the user should be alerted clearly

This avoids partially applied routing while preserving debugging information.

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
- compact working summary, updated to the newest useful version
- logs, prompts, and raw outputs

### Data that should be overwritten after a successful iteration

These values describe the latest routing result and should be replaced each time:

- active loop unit id
- last selected keyword
- last routing outcome
- phase
- working summary when a newer summary is returned
- decisions
- open questions
- blockers

### Data that should be cleared or filtered after a successful iteration

These values should not be blindly carried forward:

- open questions that were resolved in the completed iteration
- temporary unit-local notes if such notes exist
- stale blockers that no longer apply

The rule is simple:

- keep unresolved facts
- remove resolved temporary state
- do not carry stale local clutter into the next iteration

### Merge and replacement rules

The engine should use explicit normalization rules rather than ad hoc merging.

Default policy:

- `workingSummary`: replace with the latest non-empty returned summary
- `decisions`: replace with the latest normalized decision list returned by the model
- `openQuestions`: replace with the latest unresolved question set returned by the model
- `blockers`: replace with the latest unresolved blocker set returned by the model

Normalization should also:

- trim whitespace
- remove empty strings
- deduplicate exact duplicates
- preserve stable ordering as returned by the model

### Response cursor behavior after a successful iteration

The consumed user response cursor should not reset.

If response 7 was the last consumed response before or during the iteration, the next iteration should continue from that same cursor.

Otherwise the engine would re-read old user input as if it were new.

### Response cursor advancement rule

The consumed response cursor should advance only when all of the following are true:

- unread responses were included in the prompt for the current invocation
- provider output was valid
- selected keyword was valid for the active unit
- all actions succeeded
- canonical loop execution state was persisted successfully

If any of those conditions fail, the cursor must not advance.

### Self-loop example

Suppose the active loop unit is `collect_requirements` and the model returns:

- selected keyword: `[CONTINUE]`
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

- selected keyword: `[REQUIREMENTS_READY]`
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

### Completion example

Suppose the active loop unit returns:

- selected keyword: `[DONE]`
- summary: task execution is complete

Then the loop is finished.

After persistence, the state should look conceptually like this:

- phase = `done`
- last selected keyword = `[DONE]`
- last routing outcome = `done`
- session status = `completed`
- isComplete = `true`

The engine should not require an extra completion action beyond `[DONE]`.

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
        "[CONTINUE]",
        "[ASK_USER]",
        "[REQUIREMENTS_READY]",
        "[ERROR]",
        "[FAIL]",
        "[DONE]"
      ],
      "transitions": [
        {
          "keyword": "[ASK_USER]",
          "actions": ["wait-for-user"]
        },
        {
          "keyword": "[REQUIREMENTS_READY]",
          "nextUnit": "produce_tasks"
        },
        {
          "keyword": "[ERROR]"
        },
        {
          "keyword": "[FAIL]"
        },
        {
          "keyword": "[DONE]"
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

### Loop definition validation rules

The engine should reject invalid loop definitions before runtime.

Required validation:

- loop id must be non-empty
- `startUnit` must exist in `units`
- unit ids must be unique within a loop
- each unit must have at least one allowed keyword
- allowed keywords within a unit must be unique
- transition keywords within a unit must be unique
- every transition keyword must appear in that unit's `allowedKeywords`
- every `nextUnit` must be either an existing unit id or `done`
- action names must be recognized built-in actions for the current version

Invalid loop definitions should fail load deterministically.

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
    "[CONTINUE]",
    "[ASK_USER]",
    "[TASKS_READY]",
    "[ERROR]",
    "[FAIL]",
    "[DONE]"
  ],
  "transitions": [
    {
      "keyword": "[ASK_USER]",
      "actions": ["wait-for-user"]
    },
    {
      "keyword": "[TASKS_READY]",
      "nextUnit": "execute_tasks"
    },
    {
      "keyword": "[ERROR]"
    },
    {
      "keyword": "[FAIL]"
    },
    {
      "keyword": "[DONE]"
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

In normal cases, `[CONTINUE]` should be the keyword used for that behavior.

---

## Transition Structure

A transition only needs to exist when a keyword changes execution behavior or should trigger the wait-for-user behavior.

```json
{
  "keyword": "[TASKS_READY]",
  "nextUnit": "execute_tasks"
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

For v1, the only built-in action that should exist is `wait-for-user`.

---

## Output Contract

Each iteration must return strict JSON.

```json
{
  "selectedKeyword": "[ASK_USER]",
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

### Output schema rules

- `selectedKeyword` is required and authoritative
- `status` should not be included in v1 because it duplicates routing meaning and creates ambiguity
- `summary` is required and may be an empty string only if the loop explicitly allows that
- `workLog`, `questions`, `decisions`, `assumptions`, `blockers`, and `doneReason` are optional but should normalize to canonical empty values when omitted
- omitted arrays normalize to `[]`
- omitted strings normalize to `""`
- `null` should be treated as invalid for v1 output fields

### Required behavior

- exactly one `selectedKeyword`
- keyword must match the active loop unit's `allowedKeywords`
- output must be valid JSON
- invalid keyword means immediate failure
- no guessing
- no repair prompt in v1

### Invalid output handling

Malformed JSON, missing required fields, invalid field types, or an invalid keyword should produce `invalid-output` behavior.

Required handling:

- canonical loop execution state remains unchanged
- consumed response cursor does not advance
- raw output and parse or validation failure details should still be logged and persisted
- current invocation ends in failure

### Standard keyword behavior

#### `[CONTINUE]`
- successful iteration
- remain on current loop unit unless an explicit transition says otherwise
- still run normal state normalization after the iteration
- persist updated summary, decisions, questions, blockers, and response cursor normally

#### `[ERROR]`
- successful structured output indicating a concrete execution failure
- persist summary and blockers
- set phase to `error`
- set session status to `blocked`
- persist all related debugging information
- alert the user clearly
- stop the current process

#### `[FAIL]`
- successful structured output indicating the model cannot determine a valid next action
- persist summary
- set phase to `failed`
- set session status to `failed`
- stop current invocation

#### `[DONE]`
- successful structured output indicating the loop is complete
- set phase to `done`
- mark the session complete
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

1. the active loop unit selects `[ASK_USER]`
2. the engine records the returned questions through normal successful iteration persistence
3. the engine persists the current loop state
4. the engine executes `wait-for-user`
5. the engine sets phase to `waiting-for-user`
6. the current invocation stops
7. the user later runs `respond`
8. `respond` stores the user's response in the response store
9. `respond` does not run the provider and does not change the active loop unit
10. the next `loop` run resumes the same active loop unit unless a transition had already changed it
11. that next `loop` run injects unread user responses into the prompt
12. after that later loop iteration succeeds, the consumed response cursor advances

This is how a loop unit requests user input, stops cleanly, accepts a later user response, and then resumes the same logical unit with the new information.

### Respond command rules

The `respond` command should:

- only append a new user response entry
- preserve existing loop state
- preserve the current active loop unit
- preserve the current phase as `waiting-for-user`
- never consume the response by itself
- never invoke the provider by itself

The next `loop` invocation is what consumes unread responses and continues execution.

---

## Single-Writer Session Rule

Each session should be touched by only one writer or provider at a time.

This is a required runtime rule.

### Meaning

For a given session, only one active mutating operation may run at once.

Examples of mutating operations:

- `loop`
- `respond`
- any future command that writes canonical session state
- any provider-backed execution that writes loop state

### Required behavior

- the runtime must prevent overlapping writes to the same session
- a second writer must not modify the same session while another writer is active
- the runtime should fail fast with a clear message rather than allowing interleaved writes
- the engine should never allow two providers or commands to race on the same session state

This keeps routing, response consumption, and persistence deterministic.

---

## First Three Loops

The first three loop definitions should be:

### 1. Requirements Collection

Purpose:

- collect missing requirements from the user until the specification is clear enough to move forward

Typical behavior:

- active loop unit asks targeted questions
- user answers later with `respond`
- the same loop unit resumes on the next `loop` run
- when satisfied, it routes forward or ends

Suggested starting unit:

- `collect_requirements`

Suggested keywords:

- `[CONTINUE]`
- `[ASK_USER]`
- `[REQUIREMENTS_READY]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`

### 2. Requirements To Tasks

Purpose:

- turn a requirement or definition document into a concrete ordered task list

Suggested units:

- `analyze_requirements`
- `produce_tasks`

Suggested keywords:

- `[CONTINUE]`
- `[ASK_USER]`
- `[TASKS_READY]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`

### 3. Execute Tasks

Purpose:

- execute tasks until completion

Suggested units:

- `review_task_queue`
- `execute_task_batch`
- `review_results`

Suggested keywords:

- `[CONTINUE]`
- `[NEED_CLARIFICATION]`
- `[MORE_EXECUTION_NEEDED]`
- `[ERROR]`
- `[FAIL]`
- `[DONE]`

These loops are not different runtime systems.

They are different starting points into different sets of loop units.

---

## Required Persisted State

### Session state

Session state should hold:

- session identity
- selected loop id
- status
- completion status
- timestamps
- schema version

### Loop execution state

Loop execution state should hold:

- active loop unit id
- phase
- last selected keyword
- last routing outcome
- compact working summary
- decisions
- open questions
- blockers
- last consumed user response id
- last consumed user response timestamp
- schema version

That is enough to resume deterministically.

---

## Built-In Actions for V1

Built-in actions for v1 should stay minimal.

### `wait-for-user`
- used only when the loop needs user input before it can continue
- sets phase to `waiting-for-user`
- ends the current invocation after persistence

### Why v1 actions should stay minimal

The engine should not use actions as a mini command language.

For v1:

- phase changes should come from keyword handling
- summary, decisions, questions, and blockers should persist through normal successful iteration normalization
- routing should come from keywords and transitions

That means v1 does not need actions such as:

- `set-phase:<value>`
- `record-summary`
- `record-decision`
- `record-question`
- `mark-done`

### Built-in action contract

For v1, built-in actions should be deterministic and idempotent within a single invocation plan.

They should mutate only canonical loop state defined by this document.

They should not:

- execute arbitrary shell commands
- edit arbitrary files
- invoke nested providers
- depend on hidden ambient state

Not included in v1:

- arbitrary shell commands
- arbitrary file edits from JSON
- arbitrary provider chaining from JSON
- arbitrary persistence commands encoded as actions

---

## Testing Model

The future architecture should include automated tests for full loop workflows and logical-unit workflows.

These tests should verify the programmatic workflow of WallyCode end to end across one or more invocations while keeping provider behavior fully controlled.

### Testing scope

The required automated coverage is workflow-level coverage only.

That means the tests should focus on:

- full loop runs from a starting unit through one or more routing decisions
- logical-unit workflows that span repeated executions of the same active unit
- resume behavior across `loop` and `respond`
- persisted state changes across successive invocations
- routing, action execution, and normalization as observed through the public workflow behavior

This does not require a broad set of isolated low-level unit tests for every internal helper.

### Mock provider requirement

Workflow tests should use mock provider output to simulate each step.

The provider in tests should be able to:

- return fixed JSON payloads in sequence across successive calls
- simulate self-loop results such as `[CONTINUE]`
- simulate explicit transitions such as `[REQUIREMENTS_READY]` or `[TASKS_READY]`
- simulate `[ASK_USER]`, `[ERROR]`, `[FAIL]`, and `[DONE]`
- simulate invalid keyword output
- simulate malformed JSON output
- optionally inspect the prompt passed into each step so tests can verify that the workflow resumed with the correct state

The goal is to make workflow behavior deterministic without depending on a live external provider.

### Why this matters

Once provider output is fixed, routed loop behavior should be deterministic.

That means the most valuable tests are workflow tests that verify WallyCode itself:

- starts from the correct loop unit
- builds the next prompt from the correct persisted state
- consumes unread user responses correctly
- validates the selected keyword correctly
- stays on the same unit when no explicit transition exists
- transitions when an explicit transition exists
- executes built-in actions in the expected flow
- normalizes persisted state after each successful iteration
- advances the response cursor only when appropriate
- resumes correctly after `respond`
- stops correctly for `wait-for-user`, `[ERROR]`, `[FAIL]`, and `[DONE]`
- fails fast on invalid provider output
- prevents overlapping writers on the same session

### Required workflow scenarios

The documentation should treat the following as required workflow test scenarios:

- self-loop workflow where the active unit returns `[CONTINUE]` and remains active
- transition workflow where a keyword moves execution to another unit
- ask-user workflow where the engine records questions, enters `waiting-for-user`, and stops
- respond-and-resume workflow where the engine stops, `respond` stores a later user response, unread responses are injected on the next `loop` run, and the cursor advances only after that later loop iteration succeeds
- done workflow where the session is marked complete
- error workflow where blockers and summary persist and the user is alerted
- fail workflow where summary persists and execution stops
- invalid-output workflow where malformed JSON or an invalid keyword causes immediate failure
- persistence-failure workflow where canonical state remains unchanged
- action-failure workflow where canonical state remains unchanged
- resume-failure workflow where persisted state references a missing unit or invalid schema
- single-writer workflow where a second writer is rejected while the first writer is active

### Test design guidance

Each workflow test should define:

- the loop definition under test
- the starting persisted state
- the ordered mock provider outputs for each invocation
- any user responses appended between invocations
- the expected active unit after each step
- the expected phase after each step
- the expected persisted summary, decisions, questions, blockers, and response cursor
- the expected completion or stop condition

This testing model should be treated as part of the architecture, not as an optional afterthought.

---

## Logging and Observability

Each iteration log should include:

- invocation id
- loop id
- active loop unit id
- selected keyword
- whether execution stayed or transitioned
- actions executed
- next active loop unit id
- phase after routing
- summary
- done reason if any
- parse or validation outcome

---

## Safety Limits

Even with `[CONTINUE]` as the standard self-loop keyword, the runtime should still enforce practical safety limits.

Examples:

- max iterations per invocation
- max repeated self-loops without meaningful state change
- operator-visible warnings when the same unit keeps returning low-value `[CONTINUE]` results

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
- deterministic workflow testing with mocked provider output
