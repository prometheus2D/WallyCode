# WallyCode Loop Routing Design

## Purpose

This document defines the generic loop routing engine.

A loop is a named entry point into a JSON-defined set of loop units.

Runtime model:

- the user selects a loop
- the loop starts at its configured start unit
- the engine runs the active unit
- the active unit repeats by default
- a returned keyword may keep the same unit active, ask the user for input and stop, move to another unit, or end the loop

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

A loop unit may use only the keywords listed in its `allowedKeywords`.

Built-in standard keywords are still subject to this rule.

If a built-in keyword is not listed for the active unit, it is not valid for that unit.

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
The routing rule for a selected loop-specific keyword.

If no explicit transition exists, execution stays on the current unit.

Built-in standard keywords use built-in engine behavior and are not overridden by loop-defined transition logic.

### Built-In Keyword
A standard keyword with fixed engine behavior.

Current built-in standard keywords:

- `[CONTINUE]`
- `[ASK_USER]`
- `[DONE]`
- `[ERROR]`
- `[FAIL]`

Built-in keywords:

- must still appear in a unit's `allowedKeywords` to be valid there
- use engine-defined behavior
- cannot be redefined by a loop author

### `nextUnit`
The routing destination after a successful iteration.

`nextUnit` is used only for loop-specific routing keywords or explicit non-built-in transitions.

---

## Standard Keywords

### `[CONTINUE]`
Default self-loop keyword.

- iteration succeeds
- remain on current unit unless an explicit non-built-in transition overrides it
- persist normal state updates

### `[ASK_USER]`
Built-in user-input keyword.

- iteration succeeds
- remain on current unit
- stop the loop so the user can answer with `respond`
- do not move to a next unit
- preserve returned questions and normal state updates

`[ASK_USER]` is a built-in keyword, not a loop-defined routing keyword.

### `[ERROR]`
Concrete execution failure.

Examples:

- command failure
- PowerShell failure
- file operation failure
- literal programming action failure

Handling:

- persist summary and blockers when provided
- persist debugging information
- alert the user
- stop the current process

### `[FAIL]`
Routing or reasoning failure.

Examples:

- no valid keyword fits
- model cannot choose confidently
- prompt or loop definition is insufficient

Handling:

- persist summary when provided
- log clearly
- stop the current invocation
- do not guess a transition

### `[DONE]`
Explicit completion signal.

- mark the loop complete
- stop invocation
- retain `activeUnitId` as the last executed unit for auditability and simpler diagnostics

---

## Canonical Values

### Lifecycle status

- `active`
- `blocked`
- `completed`
- `failed`

### Last routing outcome

- `self-loop`
- `transition`
- `ask-user`
- `done`
- `error`
- `fail`
- `invalid-output`

---

## Design Rationale

### Why lifecycle is `status` plus `lastRoutingOutcome`

`activeUnitId` answers where work resumes.

`status` answers whether the session can continue right now.

`lastRoutingOutcome` answers why the current state was reached.

A second lifecycle field adds overlap without adding enough information to justify the extra state machine.

Keep these roles separate:

- `activeUnitId`: where the next routed step resumes
- `status`: coarse lifecycle of the session
- `lastRoutingOutcome`: what the last iteration decided
- `lastProcessedUserResponseId`: which user responses have already been consumed

This separation matters because the same unit may remain active after `[CONTINUE]`, `[ASK_USER]`, `[ERROR]`, or `[DONE]`, but the system should not need a second lifecycle enum just to explain that difference.

Use these mappings:

- waiting for user is represented as `status = blocked` plus `lastRoutingOutcome = ask-user`
- an execution problem is represented as `status = blocked` plus `lastRoutingOutcome = error`
- completion is represented as `status = completed` plus `lastRoutingOutcome = done`
- terminal failure is represented as `status = failed` plus `lastRoutingOutcome = fail` or `invalid-output`

### Why state is split

Stable session metadata and mutable loop execution state change at different rates and have different failure semantics.

Keep in session state:

- goal
- provider and model choice
- source path
- iteration counter
- selected loop or template id
- lifecycle status

Keep in loop execution state:

- active unit id
- last selected keyword
- last routing outcome
- working summary
- decisions, questions, blockers
- response-consumption cursor

The split is deliberate because it:

- lets routing behavior evolve without constantly reshaping session identity data
- keeps resume validation small and deterministic
- reduces the blast radius of a bad iteration-state write
- keeps lifecycle ownership in one place instead of duplicating it across session state and loop state

### Why `respond` has two modes

The design separates two operator intents:

- answer the current `[ASK_USER]` and continue now
- add more context without spending a provider call yet

Normal mode exists for the common human-in-the-loop path.

Store-only mode exists because operators sometimes want to stage multiple clarifications, capture context while reviewing files, or wait until they are ready to spend the next loop step.

The modes change when the next loop invocation happens, not what gets stored.

### Exact stored-response contract

The stored-response contract should be explicit and small.

- canonical response storage is `responses.json`
- human-readable history is mirrored in `memory/user-responses.md`
- each response entry is append-only and contains `id`, `timestampUtc`, and `text`
- pending responses are the entries whose `id` is greater than `lastProcessedUserResponseId`
- pending responses are supplied to the next loop prompt in ascending id order
- after a successful loop iteration, `lastProcessedUserResponseId` and `lastProcessedUserResponseAt` advance to the newest consumed entry
- a failed iteration must not advance that cursor
- response text is never rewritten in place; additional operator input always appends a new entry

Normal and store-only `respond` share the same storage contract.

The only difference between the modes is whether `respond` also starts the next loop step immediately.

Example response log:

```json
{
  "responses": [
    {
      "id": 1,
      "timestampUtc": "2025-01-01T00:00:00Z",
      "text": "Exports should support csv and pdf."
    }
  ]
}
```

### Whether built-ins are truly rigid

Yes, in the sense that the engine owns their semantics.

Rigid means:

- loop authors cannot redefine `[CONTINUE]`, `[ASK_USER]`, `[DONE]`, `[ERROR]`, or `[FAIL]`
- loop authors cannot attach custom `nextUnit` behavior to those built-ins
- the engine, not the loop definition, decides what those keywords do

Rigid does not mean every unit must expose every built-in.

Units still control access through `allowedKeywords`.

That boundary is intentional: built-ins define engine control flow, while loop-specific keywords define workflow meaning.

### Strengths being kept

The design intentionally keeps:

- bounded iterations with explicit stop points
- small inspectable state files
- append-only prompts, raw outputs, and logs for auditability
- provider-agnostic execution
- explicit human-in-the-loop response handling
- deterministic resume based on persisted state instead of prompt-only inference

The point of routing is to add explicit workflow structure without losing the current system's operator control and debuggability.

### Implementation order implied by this design

Recommended implementation order:

1. lock the persisted contracts first: session lifecycle status, execution state, response log, and response cursor
2. enforce strict iteration output parsing and built-in keyword validation
3. load routed loop definitions and persist `status`, `activeUnitId`, `lastSelectedKeyword`, and `lastRoutingOutcome` as one logical update
4. implement `[ASK_USER]` stop-and-resume using the existing response log and cursor
5. add normal `respond` auto-resume on top of that same storage contract
6. add loop-specific transitions and definition validation
7. add single-writer protection, workflow tests, and cleanup of inference paths

This order keeps the current strengths in place while changing only one control-plane concern at a time.

---

## Memory Model

Memory stores persisted facts needed for deterministic resume.

Keep:

- goal
- loop id
- lifecycle status
- active loop unit id
- compact working summary
- decisions
- open questions
- persisted user-response log
- response cursor for the last consumed user response
- prompts
- raw outputs
- logs

Do not use memory as a prose workflow inference layer.

Routing must come from:

- lifecycle status
- active unit id
- last selected keyword
- last routing outcome
- built-in keyword behavior
- transition mapping for loop-specific keywords
- response cursor when user input is involved

---

## Persisted State Shape

### Session state

```json
{
  "schemaVersion": 1,
  "sessionId": "session-001",
  "loopId": "example-loop",
  "status": "active",
  "createdAtUtc": "2025-01-01T00:00:00Z",
  "updatedAtUtc": "2025-01-01T00:00:00Z"
}
```

### Loop execution state

```json
{
  "schemaVersion": 1,
  "activeUnitId": "example_unit",
  "lastSelectedKeyword": "[CONTINUE]",
  "lastRoutingOutcome": "self-loop",
  "workingSummary": "One requirement is still unclear.",
  "decisions": ["Desktop first"],
  "openQuestions": ["One unresolved question remains."],
  "blockers": [],
  "lastProcessedUserResponseId": 3,
  "lastProcessedUserResponseAt": "2025-01-01T00:00:00Z"
}
```

Rules:

- persisted state must include `schemaVersion`
- persisted state must be forward-migratable
- missing required fields cause deterministic load failure
- the engine must not silently invent missing routing fields during resume
- session state owns lifecycle status
- loop execution state stores response-consumption metadata, not response text
- response text itself lives in the append-only response log

---

## Routing Rule

**Run the active unit again unless the selected keyword explicitly moves to another unit, asks the user for input and stops, or ends the loop.**

`[CONTINUE]` is the standard self-loop keyword.

`[ASK_USER]` is the standard built-in stop-and-wait-for-response keyword.

---

## Routing Resolution

Resolution order:

1. accept the selected keyword
2. validate it against the active unit
3. apply built-in keyword behavior when the keyword is built in
4. otherwise resolve an explicit transition if present
5. determine the next active unit
6. normalize and persist state

Rules:

- no explicit transition => remain on current unit
- `nextUnit` => move after successful iteration
- `[ASK_USER]` => persist, stop, wait for `respond`, remain on same unit
- `[DONE]` => end the loop

### Built-In Keywords vs `nextUnit`

Built-in keywords use built-in engine behavior.

`nextUnit` is for loop-specific routing.

Design rules:

- use `nextUnit` for routing to another unit
- use `[ASK_USER]` when the loop must stop and wait for user input
- `[ASK_USER]` must never set `nextUnit`
- loop authors must not override built-in keyword behavior

Examples:

```json
{
  "keyword": "[ASK_USER]"
}
```

- remain on same unit
- persist returned questions and summary normally
- stop and wait for `respond`

```json
{
  "keyword": "[SOME_ROUTING_KEYWORD]",
  "nextUnit": "another_unit"
}
```

- no ask-user behavior
- next active unit becomes `another_unit`

---

## Atomic Iteration Semantics

One loop invocation is one logical transaction.

Required rule:

- routing resolution
- state normalization
- persistence

must succeed or fail as one atomic update.

If any step after provider output parsing fails, do not partially commit:

- lifecycle status
- next active unit
- last routing outcome
- summary
- decisions
- questions
- blockers
- latest response consumption state

Diagnostic artifacts may still be persisted separately:

- raw provider output
- prompt text
- parse error details
- exception details
- invocation log entry

---

## Successful Iteration State Update

After every successful iteration:

1. accept keyword
2. resolve built-in behavior or transition
3. update lifecycle status and loop state
4. set next active unit id
5. persist updated session state and execution state
6. persist logs, prompt, raw output
7. end invocation

### Carry forward

- goal
- selected loop id
- compact working summary
- logs, prompts, raw outputs

### Overwrite

- active loop unit id
- last selected keyword
- last routing outcome
- workingSummary
- decisions
- openQuestions
- blockers

### Overwrite in session state

- lifecycle status

### Clear or filter

- resolved open questions
- temporary unit-local notes
- stale blockers
- consumed response data after a resumed loop run uses it successfully

### State cleanup rules

- `workingSummary`: replace with latest non-empty summary when provided
- `decisions`: replace with latest cleaned decision list
- `openQuestions`: replace with latest unresolved question set
- `blockers`: replace with latest unresolved blocker set
- trim whitespace
- remove empty strings
- deduplicate exact duplicates
- preserve returned ordering

### Response handling

`respond` provides the user's answer.

Default behavior:

- store the response
- in normal mode, immediately run the loop again
- include all pending stored responses in the prompt
- after a successful resumed loop run, advance the response cursor to the newest consumed entry

Optional niche behavior:

- support a hardcoded store-only mode that records additional response text without immediately running the loop
- store-only mode appends additional response entries rather than replacing prior text
- both modes share the same append-only response log

---

## Output Contract

Each iteration must return strict JSON.

```json
{
  "selectedKeyword": "[ASK_USER]",
  "summary": "One export-format question remains before requirements are complete.",
  "workLog": "Reviewed the current state and identified one remaining ambiguity.",
  "questions": ["Should exports support csv only, or csv plus pdf?"],
  "decisions": ["Desktop first", "Local storage is acceptable for v1"],
  "assumptions": [],
  "blockers": [],
  "doneReason": ""
}
```

Rules:

- `selectedKeyword` is required and authoritative
- `summary` is recommended because it gives the next run compact context
- omitted arrays normalize to `[]`
- omitted strings normalize to `""`
- `null` is invalid for v1 fields
- exactly one `selectedKeyword`
- keyword must match the active unit's `allowedKeywords`
- built-in keywords use built-in engine behavior
- output must be valid JSON
- invalid keyword means immediate failure
- no guessing
- no repair prompt

### Unknown output fields

If provider output includes extra fields not defined by the contract:

- fields needed by the engine contract must be parsed and used
- fields not needed by the engine contract should be ignored in v1
- unknown fields must not affect routing unless the contract is explicitly expanded to support them

### Invalid output handling

Malformed JSON, missing required fields, invalid field types, or invalid keywords:

- leave canonical loop state unchanged
- do not consume the stored response
- still log and persist raw output and validation failure details
- end current invocation in failure

### Standard keyword behavior

#### `[CONTINUE]`
- successful iteration
- remain on current unit unless explicitly transitioned by non-built-in routing
- set session status to `active`
- set last routing outcome to `self-loop`
- run normal state update

#### `[ASK_USER]`
- successful iteration
- persist summary and questions when provided
- stop loop execution
- remain on current unit
- do not move to a next unit
- set session status to `blocked`
- set last routing outcome to `ask-user`

#### `[ERROR]`
- successful structured execution failure
- persist summary and blockers when provided
- set session status to `blocked`
- set last routing outcome to `error`
- persist debugging information
- alert user
- stop process

#### `[FAIL]`
- successful structured routing failure
- persist summary when provided
- set session status to `failed`
- set last routing outcome to `fail`
- stop invocation

#### `[DONE]`
- successful structured completion
- set session status to `completed`
- set last routing outcome to `done`
- retain `activeUnitId` as the last executed unit
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
- pending responses from `respond`, when present

Do not include large derived workflow documents unless they add unique value not already present in structured state.

---

## User Response Flow

The routing engine must support explicit user interaction through `respond`.

Flow:

1. active unit selects `[ASK_USER]`
2. engine records returned questions through normal successful iteration persistence
3. invocation stops
4. user later runs `respond`
5. `respond` appends the user's response to the session response log
6. in routed normal mode, `respond` immediately starts the next loop run
7. in store-only mode, `respond` stops after storing the response and a later `loop` run consumes it
8. the next loop run resumes the same active unit
9. the next run includes all pending stored responses in the prompt
10. after a successful run, the response cursor advances

### `respond` rules

`respond` should:

- store a user response for the current session
- preserve existing loop state until the resumed loop run updates it
- preserve current active unit until the resumed loop run updates it
- generally be used to respond to an `[ASK_USER]` situation
- also be usable when the operator wants to provide more information
- allow the loop to continue after `[ASK_USER]`
- potentially be used to retry progress after `error` or `fail`
- support a niche store-only mode for adding more response text without immediately running the loop
- append new response entries instead of rewriting prior ones

`respond` should not:

- directly answer open questions by itself
- directly change routing by itself
- replace previously stored response text when store-only mode is used

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

## Built-In Keyword Rules

Built-in standard keywords are:

- `[CONTINUE]`
- `[ASK_USER]`
- `[DONE]`
- `[ERROR]`
- `[FAIL]`

Rules:

- a unit may use a built-in keyword only if it appears in that unit's `allowedKeywords`
- built-in keyword behavior is defined by the engine
- loop authors cannot override built-in keyword behavior
- `[ASK_USER]` always stops the loop and waits for `respond`
- `[ASK_USER]` never routes to a next unit

---

## JSON Structure

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
          "keyword": "[SOME_ROUTING_KEYWORD]",
          "nextUnit": "another_unit"
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
- built-in keywords must not appear in `transitions`
- every `nextUnit` must be an existing unit id

---

## Logging and Observability

Each iteration log should include:

- invocation id
- loop id
- lifecycle status after routing
- active loop unit id
- selected keyword
- last routing outcome after routing
- whether execution stayed or transitioned
- next active loop unit id
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

**A loop is a named starting point into a set of loop units. One loop unit is active at a time. That loop unit repeats by default. The model returns one keyword. That keyword either keeps execution on the same unit, asks the user for input and stops, moves to another unit, or ends the loop. Built-in keywords are engine-defined, but each unit still controls access to them through `allowedKeywords`.**

This gives the system:

- explicit routing
- small persisted state
- deterministic resume behavior
- clean `respond` integration
- one lifecycle field instead of overlapping lifecycle enums
- lower prompt cost
- simpler loop authoring
- less engine complexity
- deterministic workflow testing with mocked provider output
