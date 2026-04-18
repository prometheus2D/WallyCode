# WallyCode Routing Design

## Purpose

This document defines the generic routing engine.

The system is authored as logical units linked by keywords.

The persisted field name `definitionName` identifies the routing definition.

Runtime model:

- the session starts at its configured start unit
- the engine runs the active logical unit
- the active logical unit repeats by default
- a returned keyword may keep the same logical unit active, ask the user for input and stop, move to another logical unit, or end the session

This document covers engine semantics only.

Companion documents:

- `routing-examples.md`
- `routing-testing.md`
- `requirements-gathering.md`
- `task-generation.md`
- `task-execution.md`

---

## Core Model

### Routed Definition
The named set of linked logical units with:

- `name`
- `description`
- `startUnitName`
- `sharedInstructions`
- `units`

The definition `name` is its canonical identity.

The persisted field `definitionName` refers to this definition identity.

### Logical Unit
The active mode of work.

Only one logical unit is active at a time.

A unit's `name` is its canonical identity.

A logical unit may use only the keywords listed in its `allowedKeywords`.

Built-in standard keywords are subject to this rule.

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

Definition-specific keywords may also exist.

### Transition
The definition-owned mapping from a definition-specific keyword to the next unit `name`.

Definition-specific keywords do not have implicit routing behavior.

If a definition-specific keyword is valid for a unit, that unit must define a transition target for it.

Built-in standard keywords use built-in engine behavior and are not overridden by definition transition logic.

### Built-In Keyword
A standard keyword with fixed engine behavior.

Built-in standard keywords:

- `[CONTINUE]`
- `[ASK_USER]`
- `[DONE]`
- `[ERROR]`
- `[FAIL]`

Built-in keywords:

- must still appear in a unit's `allowedKeywords` to be valid there
- use engine-defined behavior
- cannot be redefined by a definition author

### `transitions`
Definition data that maps each definition-specific keyword directly to the next unit `name`.

Transition targets come from the routed definition, not iteration output.

Use the unit `name` as the canonical routing target.

---

## Standard Keywords

### `[CONTINUE]`
Default same-unit repeat keyword.

- iteration succeeds
- remain on the same unit

### `[ASK_USER]`
Built-in user-input keyword.

- iteration succeeds
- remain on current unit
- stop the current session so the user can answer with `respond`
- do not move to a next unit

`[ASK_USER]` is a built-in keyword, not a definition-defined routing keyword.

### `[ERROR]`
Concrete execution failure.

Examples:

- command failure
- PowerShell failure
- file operation failure
- literal programming action failure

Handling:

- persist debugging information
- alert the user
- stop the current invocation

### `[FAIL]`
Routing or reasoning failure.

Examples:

- no valid keyword fits
- model cannot choose confidently
- prompt or routed definition is insufficient

Handling:

- log clearly
- stop the current invocation
- do not guess a transition

### `[DONE]`
Explicit completion signal.

- mark the session complete
- stop invocation
- retain `activeUnitName` as the last executed unit for auditability and simpler diagnostics

### Successful structured iteration

If an iteration returns valid structured output, the engine applies the same state normalization rules regardless of `selectedKeyword`.

`selectedKeyword` controls lifecycle and routing.

`summary`, `questions`, `decisions`, and `blockers` overwrite persisted working state on every successful iteration.

If one of those fields is omitted or returned empty, the persisted value is cleared.

A successful iteration must return any working-state value the next logical unit still needs.

Validation failures are the only case that leave working state unchanged.

---

## Canonical Values

### Lifecycle status

- `active`
- `blocked`
- `completed`
- `failed`

### Lifecycle derivation

Each logical unit chooses its routing result by returning one valid `selectedKeyword` for that unit.

After keyword validation and transition resolution, the engine normalizes session status as follows:

- `[CONTINUE]` or a resolved definition-specific transition sets `status = active`
- `[ASK_USER]` or `[ERROR]` sets `status = blocked`
- `[DONE]` sets `status = completed`
- `[FAIL]` sets `status = failed`
- invocation validation failures leave the existing canonical values unchanged

---

## Design Rationale

### Why lifecycle is just `status`

`activeUnitName` answers where work resumes.

`status` answers whether the session can continue right now.

`lastSelectedKeyword` records what the last successful structured iteration chose.

That is enough.

Keep these roles separate:

- `activeUnitName`: where the next routed step resumes
- `status`: coarse lifecycle of the session
- `lastSelectedKeyword`: what the last successful structured iteration decided
- `lastProcessedUserResponseId`: which user responses have already been consumed

This separation matters because the same unit may remain active after `[CONTINUE]`, `[ASK_USER]`, `[ERROR]`, or `[DONE]`, but the system should not need a second lifecycle field just to explain that difference.

After the logical unit chooses a keyword and routing is resolved:

- waiting for user and execution problems are both represented as `status = blocked`
- completion is represented as `status = completed`
- terminal failure is represented as `status = failed`
- the last successful keyword explains how the session reached that status when the distinction matters
- invalid output is a validation failure that leaves canonical state unchanged and is recorded only in diagnostics

### Why canonical state is one file

All canonical mutable session data belongs in one state document.

Keep in session state:

- goal
- provider and model choice
- source path
- iteration counter
- definition identity (`definitionName`)
- lifecycle status
- active unit name
- last selected keyword
- working summary
- decisions, questions, blockers
- response-consumption cursor

Keep response text in the append-only response log.

This is deliberate because it:

- keeps resume validation small and deterministic
- avoids coordinating two canonical state writes
- reduces the chance of drift between separate state documents
- keeps routing and working state together in the single file the runtime actually resumes from

### Why `respond` resumes immediately

The design keeps one operator path after `[ASK_USER]`:

- answer with `respond`
- the engine stores the response and resumes immediately

If the operator wants to wait, they can wait before calling `respond`.

The design does not need a storage-only response mode.

### Exact stored-response contract

The stored-response contract should be explicit and small.

- canonical response storage is `responses.json`
- response text may be mirrored in `memory/user-responses.md` as an observability artifact
- each response entry is append-only and contains `id`, `timestampUtc`, and `text`
- pending responses are the entries whose `id` is greater than `lastProcessedUserResponseId`
- pending responses are supplied to the next routed prompt in ascending id order
- after a successful canonical state update, `lastProcessedUserResponseId` advances to the newest consumed entry
- a failed iteration must not advance that cursor
- response text is never rewritten in place; additional operator input always appends a new entry

`respond` uses this storage contract and immediately starts the next routed run.

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

Cursor example:

- if `lastProcessedUserResponseId = 3`, the next pending response id is `4`
- a response whose `id` matches `lastProcessedUserResponseId` has already been consumed

### Whether built-ins are truly rigid

Yes, in the sense that the engine owns their semantics.

Rigid means:

- definition authors cannot redefine `[CONTINUE]`, `[ASK_USER]`, `[DONE]`, `[ERROR]`, or `[FAIL]`
- definition authors cannot attach custom transition-target behavior to those built-ins
- the engine, not the routing definition, decides what those keywords do

Rigid does not mean every unit must expose every built-in.

Units still control access through `allowedKeywords`.

That boundary is intentional: built-ins define engine control flow, while definition-specific keywords define workflow meaning.

### Design priorities

The routing design prioritizes:

- bounded iterations with explicit stop points
- small inspectable state files
- append-only prompts, raw outputs, and logs for auditability
- provider-agnostic execution
- explicit operator response handling
- deterministic resume based on persisted state instead of prompt-only inference

---

## Canonical State Model

Canonical state stores the persisted facts needed for deterministic resume.

Keep in canonical state:

- goal
- definition identity (`definitionName`)
- lifecycle status
- active unit name
- last selected keyword
- compact working summary
- decisions
- open questions
- blockers
- response cursor for the last consumed user response

Do not use canonical state as a prose workflow inference layer.

Routing must come from:

- lifecycle status
- active unit name
- last selected keyword
- built-in keyword behavior
- transition mapping for definition-specific keywords
- response cursor when user input is involved

## Observability Artifacts

Artifacts may still be persisted for auditability:

- prompts
- raw outputs
- logs
- human-readable mirrors such as `memory/user-responses.md`

Artifacts must not drive routing.

---

## Persisted State Shape

### Session state

```json
{
  "schemaVersion": 1,
  "sessionId": "session-001",
  "definitionName": "example-definition",
  "goal": "Clarify export requirements and route into task generation.",
  "providerName": "github-copilot",
  "model": "gpt-5",
  "sourcePath": "C:\\repo\\sample-app",
  "iterationCount": 3,
  "activeUnitName": "example_unit",
  "lastSelectedKeyword": "[CONTINUE]",
  "status": "active",
  "workingSummary": "One requirement is still unclear.",
  "decisions": ["Desktop first"],
  "openQuestions": ["One unresolved question remains."],
  "blockers": [],
  "lastProcessedUserResponseId": 3,
  "createdAtUtc": "2025-01-01T00:00:00Z",
  "updatedAtUtc": "2025-01-01T00:00:00Z"
}
```

Rules:

- persisted state must include `schemaVersion`
- persisted state must be forward-migratable
- missing required fields cause deterministic load failure
- the engine must not silently invent missing routing fields during resume
- resuming requires the loaded routing definition `name` to match persisted `definitionName`
- resuming requires the loaded routing definition to contain the persisted `activeUnitName`
- if either is missing, resume fails deterministically
- editing a routing definition during an active session is unsupported
- session state stores response-consumption metadata, not response text
- response text itself lives in the append-only response log

---

## Canonical Runtime Surface

The routing runtime has three canonical persisted surfaces:

- routing definition
- session state
- response log

Observability artifacts such as prompts, raw outputs, logs, and human-readable mirrors are separate from the canonical runtime surface.

The engine reads the canonical surfaces, validates them, and derives a normalized prompt input payload for the active unit.

The model does not inspect persisted files directly.

### Prompt input payload

The prompt input payload is the reasoning-facing runtime contract.

```json
{
  "definitionName": "example-definition",
  "goal": "Clarify export requirements and route into task generation.",
  "status": "blocked",
  "lastSelectedKeyword": "[ASK_USER]",
  "activeUnit": {
    "name": "collect_requirements",
    "purpose": "Resolve missing specification details.",
    "instructions": "Choose exactly one keyword.",
    "allowedKeywords": [
      "[CONTINUE]",
      "[ASK_USER]",
      "[REQUIREMENTS_READY]",
      "[ERROR]",
      "[FAIL]",
      "[DONE]"
    ]
  },
  "workingSummary": "One export-format question remains.",
  "decisions": ["Desktop first"],
  "openQuestions": ["Should exports support csv and pdf?"],
  "blockers": [],
  "pendingResponses": [
    {
      "id": 4,
      "timestampUtc": "2025-01-01T00:00:00Z",
      "text": "Exports should support csv and pdf."
    }
  ]
}
```

Rules:

- the engine constructs the prompt input payload programmatically from session state, the active unit definition, and pending responses
- the rendered prompt may be markdown or plain text, but it must represent this normalized payload
- not every persisted field is a prompt input; reasoning inputs are limited to the fields needed by the active unit
- fields such as `sessionId`, `providerName`, `model`, `sourcePath`, timestamps, lock metadata, and response cursor remain persisted runtime metadata unless a logical unit explicitly requires them
- observability artifacts are not prompt inputs

---

## Routing Rule

**Run the active logical unit again unless the selected keyword explicitly moves to another logical unit, asks the user for input and stops, or ends the session.**

`[CONTINUE]` is the standard same-unit repeat keyword.

`[ASK_USER]` is the standard built-in stop-and-wait-for-response keyword.

---

## Routing Resolution

Resolution order:

1. accept the selected keyword
2. validate it against the active unit
3. apply built-in keyword behavior when the keyword is built in
4. otherwise resolve the explicit transition target
5. determine the next active unit
6. normalize and persist state

Rules:

- a definition-specific keyword must have an explicit transition target in the active unit definition
- a resolved definition transition target moves to that target after successful iteration
- `[ASK_USER]` persists normalized state, stops, waits for `respond`, and remains on the same unit
- `[DONE]` ends the session after normal successful state normalization

### Transition Resolution Details

The provider returns only `selectedKeyword` as the routing signal.

The engine resolves any transition from the active unit definition.

Design rules:

- the model does not emit a transition target
- the engine resolves the target unit `name` from the routing definition when a definition-specific keyword has a matching transition
- use `[CONTINUE]` for an intentional same-unit repeat
- use `[ASK_USER]` when the current session must stop and wait for user input
- definition authors must not override built-in keyword behavior

Examples:

```json
{
  "selectedKeyword": "[ASK_USER]"
}
```

- remain on same unit
- persist returned `summary` and `questions`, then normalize them into `workingSummary` and `openQuestions`
- stop and wait for `respond`

If `selectedKeyword` is `[SOME_ROUTING_KEYWORD]` and the routing definition maps that keyword to `another_unit`:

- no ask-user behavior
- next active unit becomes `another_unit`

---

## Atomic Iteration Semantics

One routed invocation is one logical transaction.

Required rule:

- routing resolution
- state normalization
- persistence

must succeed or fail as one atomic update.

If any step after provider output parsing fails, do not partially commit:

- lifecycle status
- next active unit
- last selected keyword
- workingSummary
- decisions
- openQuestions
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
3. update lifecycle status and working state
4. set next active unit name
5. persist updated session state
6. persist logs, prompt, raw output
7. end invocation

### Carry forward

- goal
- definition identity (`definitionName`)
- provider and model choice
- source path

### Overwrite

- lifecycle status
- active logical unit name
- last selected keyword
- workingSummary
- decisions
- openQuestions
- blockers

### Clear or filter

- any omitted or empty working-state field is cleared on a successful iteration
- resolved open questions disappear unless they are returned again
- stale blockers disappear unless they are returned again
- temporary unit-local notes do not carry forward unless they are returned as canonical working state
- consumed response data disappears from pending state after a resumed routed run uses it successfully

### State cleanup rules

- `workingSummary`: persist the normalized `summary`; omitted or empty => `""`
- `decisions`: persist the normalized `decisions`; omitted => `[]`
- `openQuestions`: persist the normalized `questions`; omitted => `[]`
- `blockers`: persist the normalized `blockers`; omitted => `[]`
- trim whitespace
- remove empty strings
- deduplicate exact duplicates
- preserve returned ordering
- do not implicitly carry forward stale working-state data; a successful iteration must return anything the next logical unit still needs

### Output-to-state normalization

- any successful structured iteration applies the same normalization rules regardless of `selectedKeyword`
- normalized output replaces prior persisted working state for `workingSummary`, `decisions`, `openQuestions`, and `blockers`
- any omitted or empty working-state field clears the prior persisted value
- output `selectedKeyword` becomes persisted `lastSelectedKeyword`
- output `summary` becomes persisted `workingSummary`
- output `questions` becomes persisted `openQuestions`
- output `decisions` becomes persisted `decisions`
- output `blockers` becomes persisted `blockers`
- resolved routing sets persisted `status` and `activeUnitName`

### Response handling

`respond` stores the user's answer and immediately resumes the routed session.

- store the response
- require session status `blocked`
- immediately run the routed session again
- include all pending stored responses in the resumed prompt
- after a successful resumed routed run, advance `lastProcessedUserResponseId` to the newest consumed entry
- if the resumed routed run fails, do not advance the response cursor
- append a new response entry instead of rewriting prior response text

---

## Output Contract

Each iteration must return strict JSON.

```json
{
  "selectedKeyword": "[ASK_USER]",
  "summary": "One export-format question remains before requirements are complete.",
  "questions": ["Should exports support csv only, or csv plus pdf?"],
  "decisions": ["Desktop first", "Local storage is acceptable for v1"],
  "blockers": []
}
```

Rules:

- `selectedKeyword` is required and authoritative
- routing destinations never come from provider output
- `summary` is recommended because it gives the next run compact context
- omitted arrays normalize to `[]` and clear the prior persisted array
- omitted strings normalize to `""` and clear the prior persisted string
- `null` is invalid for v1 fields
- a successful iteration must return any working-state value the next logical unit still needs
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
- reserved routing-control fields that attempt to specify a transition target must be rejected because transition targets come from the routing definition, not the model output
- unknown fields must not affect routing unless the contract is explicitly expanded to support them

### Invalid output handling

Malformed JSON, missing required fields, invalid field types, or invalid keywords:

- are invocation validation failures, not persisted routing outcomes
- leave canonical session state unchanged
- do not change lifecycle status, `activeUnitName`, `lastSelectedKeyword`, `workingSummary`, `decisions`, `openQuestions`, `blockers`, or response cursor
- do not consume the stored response
- still log and persist raw output and validation failure details
- end current invocation in failure

### Standard keyword behavior

#### `[CONTINUE]`
- successful iteration
- remain on the same unit
- set session status to `active`
- apply normal state normalization

#### `[ASK_USER]`
- successful iteration
- stop current session execution
- remain on current unit
- do not move to a next unit
- set session status to `blocked`
- apply normal state normalization

#### `[ERROR]`
- successful structured execution failure
- set session status to `blocked`
- apply normal state normalization
- persist debugging information
- alert user
- stop invocation

#### `[FAIL]`
- successful structured routing failure
- set session status to `failed`
- apply normal state normalization
- stop invocation

#### `[DONE]`
- successful structured completion
- set session status to `completed`
- retain `activeUnitName` as the last executed unit
- apply normal state normalization
- stop invocation

---

## Prompt Contents

Each iteration prompt is the rendered form of the normalized prompt input payload.

That rendered prompt should include:

- shared definition instructions
- `definitionName`
- `goal`
- `status`
- `lastSelectedKeyword`
- `activeUnit.name`
- `activeUnit.purpose`
- `activeUnit.instructions`
- `activeUnit.allowedKeywords`
- `workingSummary`
- `decisions`
- `openQuestions`
- `blockers`
- `pendingResponses`

The engine loads session state, the active unit definition, and pending responses, then injects that normalized payload into the prompt.

The LLM does not read `session.json` or response-log files directly.

Do not include large derived workflow documents when the same information is already present in structured state.

---

## User Response Flow

The routing engine must support explicit user interaction through `respond`.

`respond` stores the response and resumes the session immediately.

Flow:

1. active unit selects `[ASK_USER]`
2. engine records returned questions through normal successful iteration persistence
3. invocation stops
4. user later runs `respond`
5. `respond` appends the user's response to the session response log
6. `respond` immediately starts the next routed run
7. the next routed run resumes the same active unit
8. the next routed run includes all pending stored responses in the prompt
9. after a successful run, the response cursor advances

### `respond` rules

`respond` should:

- store a user response for the current session
- preserve existing session state until the resumed routed run updates it
- preserve current active unit until the resumed routed run updates it
- require session status `blocked`
- after `[ASK_USER]`, use `respond` as the expected recovery path
- after `[ERROR]`, default to fixing the execution problem and starting another routed run; use `respond` only when the operator wants to attach extra context before that retry
- append new response entries instead of rewriting prior ones

`respond` should not:

- directly answer open questions by itself
- directly change routing by itself
- reopen a `completed` or `failed` session

---

## Single-Writer Session Rule

Each session may be touched by only one writer or provider at a time.

Mutating operations include:

- session execution
- `respond`
- any future action that writes canonical session state
- any provider-backed execution that writes session state

Required behavior:

- prevent overlapping writes to the same session
- reject a second writer while another writer is active
- fail fast with a clear message rather than allowing interleaved writes
- never allow two providers or commands to race on the same session state

### Lock contract

- the canonical session lock file is `session.lock.json` adjacent to `session.json`
- the lock file contains `lockId`, `ownerOperation`, `acquiredAtUtc`, `renewedAtUtc`, and `expiresAtUtc`
- a writer acquires the lock by atomically creating the lock file, or atomically replacing it only after `expiresAtUtc` has passed
- an active writer must renew `renewedAtUtc` and `expiresAtUtc` while it still owns the session
- a second writer that sees an unexpired lock must fail without mutating canonical state
- a stale lock may be replaced after expiry and is treated as abandoned
- `respond` keeps the same lock across both the response append and the resumed routed run
- the writer releases the lock after the final canonical state write for that operation path

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
- definition authors cannot override built-in keyword behavior
- `[ASK_USER]` always stops the current session and waits for `respond`
- `[ASK_USER]` never routes to a next unit

---

## JSON Structure

```json
{
  "name": "example-definition",
  "description": "Example routing definition.",
  "startUnitName": "example_unit",
  "sharedInstructions": "Stay concrete.",
  "units": [
    {
      "name": "example_unit",
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
      "transitions": {
        "[SOME_ROUTING_KEYWORD]": "another_unit"
      }
    },
    {
      "name": "another_unit",
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

- definition `name` must be non-empty
- `startUnitName` must exist in `units`
- unit `name` values must be unique within a routed definition
- each unit must have at least one allowed keyword
- allowed keywords within a unit must be unique
- transition keys within a unit must be unique
- every transition key must appear in that unit's `allowedKeywords`
- every definition-specific keyword in `allowedKeywords` must appear in `transitions`
- built-in keywords must not appear in `transitions`
- every transition target unit name must be an existing unit name

---

## Logging and Observability

Each iteration log should include:

- invocation id
- definition name
- lifecycle status after routing
- active logical unit name before routing
- selected keyword
- next active logical unit name
- summary
- parse or validation outcome

---

## Safety Limits

The runtime should still enforce practical safeguards:

- max iterations per invocation
- max repeated same-unit runs without meaningful state change
- operator-visible warnings when the same unit keeps returning low-value `[CONTINUE]` results

These safeguards do not change the routing model.

---

## Final Direction

**The system is a linked set of logical units. One logical unit is active at a time. That logical unit repeats by default. The model returns one keyword. That keyword either keeps execution on the same logical unit, asks the user for input and stops, moves to another logical unit, or ends the session. Built-in keywords are engine-defined, but each logical unit still controls access to them through `allowedKeywords`.**

This gives the system:

- explicit routing
- one canonical session state file plus an append-only response log
- deterministic resume behavior
- clean `respond` integration
- a single lifecycle status
- lower prompt cost
- simpler logical-unit authoring
- less engine complexity
- deterministic workflow testing with mocked provider output
