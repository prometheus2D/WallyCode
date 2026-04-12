# WallyCode Loop Routing Design

## Purpose

This document defines the generic loop routing engine.

A loop is a named entry point into a JSON-defined set of loop units.

Runtime model:

- the user selects a loop
- the loop starts at its configured start unit
- the engine runs the active unit
- the active unit repeats by default
- a returned keyword may keep the same unit active, wait for user input, move to another unit, or end the loop

This document covers engine semantics only.

Companion documents:

- `loop-routing-examples.md`
- `loop-testing.md`
- `loop-type-requirements-gathering.md`
- `loop-type-task-generating.md`
- `loop-type-task-executing.md`

---

## Core Model

### Loop
A named workflow entry point with:

- loop id
- name
- description
- start unit id
- shared instructions
- loop units

### Loop Unit
The current mode of work.

Only one loop unit is active at a time.

### Keyword
The single machine-readable routing decision returned by the model.

Format:

- uppercase
- square brackets

Examples:

- `[CONTINUE]`
- `[ASK_USER]`
- `[DONE]`
- `[ERROR]`
- `[FAIL]`

Loop-specific keywords may also exist.

### Transition
The routing rule for a selected keyword.

If no explicit transition exists, execution stays on the current unit.

### Action
A built-in engine behavior.

Current action set:

- `wait-for-user`

Actions are not a general command system.

### `nextUnit`
The routing destination after a successful iteration.

- action = what the engine does
- `nextUnit` = where the engine goes next

---

## Standard Keywords

### `[CONTINUE]`
Default self-loop keyword.

- iteration succeeds
- remain on current unit unless an explicit transition overrides it
- persist normal state updates

### `[ERROR]`
Concrete execution failure.

Examples:

- command failure
- PowerShell failure
- file operation failure
- literal programming action failure

Required handling:

- persist summary and blockers
- persist debugging information
- alert the user
- stop the current process

### `[FAIL]`
Routing or reasoning failure.

Examples:

- no valid keyword fits
- model cannot choose confidently
- prompt or loop definition is insufficient

Required handling:

- persist summary
- log clearly
- stop the current invocation
- do not guess a transition

### `[DONE]`
Explicit completion signal.

No extra completion action is required.

---

## Canonical Runtime Values

### Session status

- `active`
- `completed`
- `failed`
- `blocked`

### Loop phase

- `active`
- `waiting-for-user`
- `done`
- `error`
- `failed`

### Routing outcome

- `self-loop`
- `transition`
- `waiting-for-user`
- `done`
- `error`
- `fail`
- `invalid-output`

---

## Memory Model

Memory stores persisted facts needed for deterministic resume.

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

Do not use memory as a prose workflow inference layer.

Routing must come from:

- active unit id
- selected keyword
- transition mapping
- explicit phase state

---

## Persisted State Shape

### Session state

```json
{
  "schemaVersion": 1,
  "sessionId": "session-001",
  "loopId": "example-loop",
  "status": "active",
  "isComplete": false,
  "createdAtUtc": "2025-01-01T00:00:00Z",
  "updatedAtUtc": "2025-01-01T00:00:00Z"
}
```

### Loop execution state

```json
{
  "schemaVersion": 1,
  "activeUnitId": "example_unit",
  "phase": "active",
  "lastSelectedKeyword": "[CONTINUE]",
  "lastRoutingOutcome": "self-loop",
  "workingSummary": "One requirement is still unclear.",
  "decisions": ["Desktop first"],
  "openQuestions": ["One unresolved question remains."],
  "blockers": [],
  "lastConsumedUserResponseId": 4,
  "lastConsumedUserResponseTimestampUtc": "2025-01-01T00:00:00Z"
}
```

Rules:

- persisted state must include `schemaVersion`
- persisted state must be forward-migratable
- missing required fields cause deterministic load failure
- the engine must not silently invent missing routing fields during resume

---

## Routing Rule

**Run the active unit again unless the selected keyword explicitly transitions to another unit, waits for user input, or ends the loop.**

`[CONTINUE]` is the standard self-loop keyword.

---

## Routing Resolution

Resolution order:

1. accept the selected keyword
2. validate it against the active unit
3. resolve an explicit transition if present
4. determine the next active unit
5. execute any built-in action
6. normalize and persist state

Rules:

- no explicit transition => remain on current unit
- `nextUnit` => move after successful iteration
- `wait-for-user` => persist, stop, wait for later `respond`
- `[DONE]` => end the loop

### `nextUnit` vs `actions`

`nextUnit` selects the next logical unit.

`actions` trigger built-in engine behavior.

Current design:

- use `nextUnit` for routing to another unit
- use `wait-for-user` only when the same unit must pause for user input
- avoid combining `nextUnit` and `wait-for-user` unless there is a clear product need

Examples:

```json
{
  "keyword": "[ASK_USER]",
  "actions": ["wait-for-user"]
}
```

- remain on same unit
- persist returned questions and summary normally
- set phase to `waiting-for-user`
- stop and wait for `respond`

```json
{
  "keyword": "[SOME_ROUTING_KEYWORD]",
  "nextUnit": "another_unit"
}
```

- no wait-for-user behavior
- next active unit becomes `another_unit`

---

## Atomic Iteration Semantics

One loop invocation is one logical transaction.

Required rule:

- routing resolution
- action execution
- state normalization
- cursor advancement
- persistence

must succeed or fail as one atomic update.

If any step after provider output parsing fails, do not partially commit:

- next active unit
- phase
- summary
- decisions
- questions
- blockers
- consumed response cursor

Diagnostic artifacts may still be persisted separately:

- raw provider output
- prompt text
- parse error details
- exception details
- invocation log entry

If a built-in action fails:

- invocation fails
- canonical loop state remains unchanged
- consumed response cursor does not advance
- failure details are logged and persisted
- user is alerted clearly

---

## Successful Iteration Normalization

After every successful iteration:

1. accept keyword
2. resolve transition
3. execute built-in actions
4. normalize loop state
5. set next active unit id
6. persist updated execution state
7. persist logs, prompt, raw output
8. end invocation

### Carry forward

- goal
- selected loop id
- append-only user response history
- last consumed user response id
- last consumed user response timestamp
- compact working summary
- logs, prompts, raw outputs

### Overwrite

- active loop unit id
- last selected keyword
- last routing outcome
- phase
- working summary
- decisions
- open questions
- blockers

### Clear or filter

- resolved open questions
- temporary unit-local notes
- stale blockers

### Normalization rules

- `workingSummary`: replace with latest non-empty summary
- `decisions`: replace with latest normalized decision list
- `openQuestions`: replace with latest unresolved question set
- `blockers`: replace with latest unresolved blocker set
- trim whitespace
- remove empty strings
- deduplicate exact duplicates
- preserve returned ordering

### Response cursor advancement

Advance the consumed response cursor only if all are true:

- unread responses were included in the prompt
- provider output was valid
- selected keyword was valid for the active unit
- all actions succeeded
- canonical loop state persisted successfully

Otherwise do not advance it.

---

## Output Contract

Each iteration must return strict JSON.

```json
{
  "selectedKeyword": "[ASK_USER]",
  "summary": "One export-format question remains before requirements are complete.",
  "workLog": "Reviewed unread user responses and narrowed the remaining ambiguity to export format.",
  "questions": ["Should exports support csv only, or csv plus pdf?"],
  "decisions": ["Desktop first", "Local storage is acceptable for v1"],
  "assumptions": [],
  "blockers": [],
  "doneReason": ""
}
```

Rules:

- `selectedKeyword` is required and authoritative
- `summary` is required unless the loop explicitly allows empty
- omitted arrays normalize to `[]`
- omitted strings normalize to `""`
- `null` is invalid for v1 fields
- exactly one `selectedKeyword`
- keyword must match the active unit's `allowedKeywords`
- output must be valid JSON
- invalid keyword means immediate failure
- no guessing
- no repair prompt

### Invalid output handling

Malformed JSON, missing required fields, invalid field types, or invalid keywords:

- leave canonical loop state unchanged
- do not advance consumed response cursor
- still log and persist raw output and validation failure details
- end current invocation in failure

### Standard keyword behavior

#### `[CONTINUE]`
- successful iteration
- remain on current unit unless explicitly transitioned
- run normal normalization

#### `[ERROR]`
- successful structured execution failure
- persist summary and blockers
- set phase to `error`
- set session status to `blocked`
- persist debugging information
- alert user
- stop process

#### `[FAIL]`
- successful structured routing failure
- persist summary
- set phase to `failed`
- set session status to `failed`
- stop invocation

#### `[DONE]`
- successful structured completion
- set phase to `done`
- mark session complete
- stop invocation

---

## Prompt Contents

Each iteration prompt should include:

- shared loop instructions
- active unit id and title
- active unit purpose
- active unit instructions
- allowed keywords
- explicit instruction to choose exactly one keyword
- goal
- compact working summary
- recorded decisions
- recorded open questions
- unread user responses only

Do not include large derived workflow documents unless they add unique value not already present in structured state.

---

## User Response Flow

The routing engine must support explicit user interaction through `respond`.

Flow:

1. active unit selects `[ASK_USER]`
2. engine records returned questions through normal successful iteration persistence
3. engine persists current loop state
4. engine executes `wait-for-user`
5. engine sets phase to `waiting-for-user`
6. invocation stops
7. user later runs `respond`
8. `respond` stores the user response
9. `respond` does not run the provider
10. `respond` does not change the active unit
11. next `loop` run resumes the same active unit unless a transition had already changed it
12. next `loop` run injects unread responses into the prompt
13. consumed response cursor advances only after that later loop iteration succeeds

### `respond` rules

`respond` should:

- append a new user response entry
- preserve existing loop state
- preserve current active unit
- preserve phase as `waiting-for-user`
- never consume the response by itself
- never invoke the provider by itself

---

## Single-Writer Session Rule

Each session may be touched by only one writer or provider at a time.

Mutating operations include:

- `loop`
- `respond`
- any future command that writes canonical session state
- any provider-backed execution that writes loop state

Required behavior:

- prevent overlapping writes to the same session
- reject a second writer while another writer is active
- fail fast with a clear message rather than allowing interleaved writes
- never allow two providers or commands to race on the same session state

---

## Built-In Actions

Current built-in actions:

### `wait-for-user`
- used only when the loop needs user input before it can continue
- sets phase to `waiting-for-user`
- ends the current invocation after persistence

Action contract:

- deterministic
- idempotent within a single invocation plan
- mutates only canonical loop state defined here
- must not execute arbitrary shell commands
- must not edit arbitrary files
- must not invoke nested providers
- must not depend on hidden ambient state

Not supported:

- arbitrary shell commands
- arbitrary file edits from JSON
- arbitrary provider chaining from JSON
- arbitrary persistence commands encoded as actions

---

## Future JSON Structure

```json
{
  "id": "example-loop",
  "name": "Example Loop",
  "description": "Example routed loop definition.",
  "startUnit": "example_unit",
  "sharedInstructions": "Stay concrete.",
  "units": [
    {
      "id": "example_unit",
      "title": "Example Unit",
      "purpose": "Demonstrate generic routing.",
      "instructions": "Choose exactly one keyword.",
      "allowedKeywords": [
        "[CONTINUE]",
        "[ASK_USER]",
        "[SOME_ROUTING_KEYWORD]",
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
          "keyword": "[SOME_ROUTING_KEYWORD]",
          "nextUnit": "another_unit"
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
    },
    {
      "id": "another_unit",
      "title": "Another Unit",
      "purpose": "Demonstrate a transition target.",
      "instructions": "Choose exactly one keyword.",
      "allowedKeywords": [
        "[CONTINUE]",
        "[DONE]",
        "[ERROR]",
        "[FAIL]"
      ]
    }
  ]
}
```

Validation rules:

- loop id must be non-empty
- `startUnit` must exist in `units`
- unit ids must be unique within a loop
- each unit must have at least one allowed keyword
- allowed keywords within a unit must be unique
- transition keywords within a unit must be unique
- every transition keyword must appear in that unit's `allowedKeywords`
- every `nextUnit` must be either an existing unit id or `done`
- action names must be recognized built-in actions

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

The runtime should still enforce practical safeguards:

- max iterations per invocation
- max repeated self-loops without meaningful state change
- operator-visible warnings when the same unit keeps returning low-value `[CONTINUE]` results

These safeguards do not change the routing model.

---

## Final Direction

**A loop is a named starting point into a set of loop units. One loop unit is active at a time. That loop unit repeats by default. The model returns one keyword. That keyword either keeps execution on the same unit, waits for user input, moves to another unit, or ends the loop.**

This gives the system:

- explicit routing
- small persisted state
- deterministic resume behavior
- clean `respond` integration
- lower prompt cost
- simpler loop authoring
- less engine complexity
- deterministic workflow testing with mocked provider output
