# WallyCode Loop Routing Design

## Purpose

This document defines the next evolution of WallyCode loops.

Today, WallyCode loops are iterative memory-driven sessions:

- a loop template provides a system prompt and initial memory
- each iteration sends memory plus metadata to the provider
- the provider returns structured JSON
- WallyCode updates memory and session state
- the user can respond between iterations

That model is already useful, but it is still mostly a **single generic loop**.

The next step is to make loops into a **simple JSON-defined routing system** where:

- a loop is made of named steps
- each step tells the LLM what kind of decision or work to do
- the LLM returns one or more routing keywords in structured output
- those keywords determine the next step, next loop, or a built-in action
- memory remains a minimal persistent context layer across the whole session

The goal is to keep the system:

- simple to author in JSON
- robust enough for real workflows
- memory-light between iterations
- token-efficient
- deterministic at the routing layer
- compatible with one-step-at-a-time loop execution plus user responses between runs
- extensible without embedding workflow logic into C# for every new loop

---

## Current Application Review

## What exists today

The current loop system already has strong foundations.

### 1. Loop templates are JSON-driven

Current templates live under:

- `WallyCode.Console/Templates/Loops/default.json`
- `WallyCode.Console/Templates/Loops/requirements.json`

Current `LoopTemplate` supports:

- `name`
- `description`
- `systemPrompt`
- `responseSchemaPrompt`
- initial memory documents
- optional `stopKeyword`

This is a good starting point because loop behavior is already partially data-driven.

### 2. Memory is already externalized and persistent

`MemoryWorkspace` persists:

- session state
- loop state
- prompts
- raw outputs
- logs
- memory documents
- user responses

This is exactly the right substrate for routed loops.

### 3. The loop runner already separates concerns reasonably well

Current flow:

1. load template
2. load loop state
3. read memory snapshot
4. build prompt
5. call provider
6. parse structured response
7. update loop state
8. render memory
9. persist session

This means the architecture is already close to a generic engine.

### 4. Structured output already exists

The current prompt requires JSON-only output with fields like:

- `status`
- `summary`
- `workLog`
- `questions`
- `decisions`
- `assumptions`
- `blockers`
- `doneReason`

That is important because routing should also be driven by structured output, not free text.

---

## Current Limitations

The current loop model has several limitations that the new routing design should solve.

### 1. Only one implicit step exists

Even though templates differ, the engine itself behaves like one generic repeated step.

There is no explicit concept of:

- current step id
- step-local instructions
- step-local routing rules
- transitions between steps
- nested loops or subflows

### 2. Routing is mostly inferred, not declared

Today the loop decides only a few things:

- continue vs done
- waiting-for-user vs active
- stop keyword matched or not

That is too limited for richer workflows.

### 3. Memory is doing too much semantic work

Memory currently carries:

- context
- progress
- open questions
- decisions
- implicit workflow state

Memory should remain important, but workflow routing should become explicit and machine-readable.

### 4. Token usage can grow unnecessarily

If the LLM must repeatedly infer the workflow from large memory documents, token usage rises and routing becomes less deterministic.

A step-based routing model can reduce this by:

- narrowing the prompt to the current step
- using explicit allowed keywords
- using compact transition rules

---

## Design Goals

The new routing system should satisfy the following goals.

### Primary goals

1. **Loops are defined in JSON**
   - no custom C# logic per workflow
   - workflows should be programmable by editing JSON files

2. **Routing is keyword-driven**
   - the LLM emits explicit routing keywords
   - WallyCode maps those keywords to transitions or actions

3. **The routed runtime stays memory-light**
   - keep only canonical memory that cannot be reconstructed from routed state or the loop definition
   - prefer generated artifacts over rewritten prompt-source markdown documents
   - keep user responses append-only and compact

4. **The engine is deterministic where possible**
   - the LLM chooses from declared keywords
   - WallyCode validates the keyword
   - WallyCode performs the transition/action deterministically

5. **Prompt token usage is reduced where possible**
   - step-local instructions replace repeated generic reasoning
   - routing rules are compact and explicit

6. **Single-step loop execution remains first-class**
   - one `loop` invocation should remain a valid complete unit of routed execution
   - `respond` should fit cleanly between loop invocations

### Secondary goals

7. **Safe built-in actions**
   - actions should be constrained and explicit
   - avoid arbitrary scripting in v1

8. **Good observability**
   - logs should show step, keyword, transition, and action results

9. **Forward-only design**
   - the new routed loop schema is the source of truth
   - legacy schema support is not a goal
   - the implementation should optimize for the new model, not preserve the old one

10. **Remove obsolete code during implementation**
   - legacy loop code should be deleted, not preserved
   - transitional compatibility layers should be avoided
   - unused models, parsers, and template paths should be removed once replaced

---

## Non-Goals for V1

To keep the first version simple and robust, the following should not be part of v1.

1. Arbitrary shell execution from JSON
2. Arbitrary code execution from JSON
3. Full scripting language inside loop definitions
4. Dynamic prompt templating language beyond simple placeholders
5. Multi-keyword branching with complex boolean expressions
6. Nested loop invocation as a required v1 feature
7. Legacy schema compatibility
8. Migration tooling for older loop definitions
9. Transitional code paths kept only for safety

Nested loops may be added later, but should not be required to make the first routed-loop engine useful.

---

## Core Mental Model

A routed loop is a **state machine with memory**.

### Memory answers:

- what has happened
- what the user said
- what decisions were made
- what the current context is

### Routing answers:

- what step is active now
- what keyword the LLM selected
- what action should happen next
- what step should run next

This separation is important.

Memory is semantic continuity.
Routing is execution continuity.

---

## Proposed V1 Architecture

## Top-level concepts

### 1. Loop Definition

A loop definition is a JSON file that declares:

- loop id
- name
- description
- start step id
- optional shared instructions
- step definitions
- optional built-in defaults

### 2. Step Definition

A step defines one bounded unit of reasoning.

A step should contain:

- `id`
- `title`
- `purpose`
- `instructions`
- `allowedKeywords`
- `transitions`
- optional `actions`
- optional `completionRules`

### 3. Keyword

A keyword is a compact machine-readable token emitted by the LLM.

Examples:

- `continue_work`
- `need_user_input`
- `requirements_clear`
- `done`
- `retry_same_step`

Keywords should be:

- explicit
- normalized
- finite per step
- validated by the engine

### 4. Transition

A transition maps a keyword to the next engine behavior.

Examples:

- keyword -> next step
- keyword -> same step
- keyword -> done
- keyword -> built-in action then next step

### 5. Built-in Action

An action is a small deterministic engine behavior.

Examples for v1:

- `set-phase`
- `mark-done`
- `record-summary`
- `record-decision`
- `record-question`
- `wait-for-user`

Actions should be safe, bounded, and internal.

---

## Proposed JSON Shape

This is a proposed v1 shape, not final syntax.

```json
{
  "name": "Requirements Loop",
  "description": "Collect missing requirements and route based on explicit LLM decisions.",
  "startStep": "analyze_requirements",
  "sharedInstructions": "Keep questions minimal and high value.",
  "steps": [
    {
      "id": "analyze_requirements",
      "title": "Analyze Requirements",
      "purpose": "Find the highest-value ambiguity or decision point.",
      "instructions": "Review memory and pending user responses. Choose exactly one routing keyword.",
      "allowedKeywords": [
        "ask_user",
        "requirements_clear",
        "continue_analysis"
      ],
      "transitions": [
        {
          "keyword": "ask_user",
          "actions": [ "wait-for-user" ],
          "nextStep": "analyze_requirements"
        },
        {
          "keyword": "continue_analysis",
          "nextStep": "analyze_requirements"
        },
        {
          "keyword": "requirements_clear",
          "nextStep": "done"
        }
      ]
    }
  ]
}
```

This shape is intentionally simple.

The engine should not need custom code for each loop.

---

## Proposed LLM Output Contract

The routing system depends on strict structured output.

The current response schema should evolve to include routing fields.

### Proposed v1 response shape

```json
{
  "status": "continue",
  "selectedKeyword": "ask_user",
  "summary": "One sentence summary.",
  "workLog": "Markdown work log.",
  "questions": ["What platform should this target?"],
  "decisions": [],
  "assumptions": [],
  "blockers": [],
  "doneReason": ""
}
```

### Required routing fields

- `selectedKeyword`
  - exactly one keyword in v1
  - must match one of the current step's allowed keywords

### Why exactly one keyword in v1

This keeps the engine simple and deterministic.

It avoids:

- ambiguous branching
- priority rules
- boolean routing expressions
- action ordering complexity

Multiple keywords can be considered later if needed.

---

## Routing Semantics

## V1 routing rules

For each iteration:

1. load current step id from session/loop state
2. build prompt for that step
3. provider returns structured JSON
4. parse `selectedKeyword`
5. validate keyword against current step
6. find matching transition
7. execute built-in actions
8. move to next step or mark done
9. persist updated state and memory

### Transition targets

V1 should support:

- `nextStep: <step-id>`
- `nextStep: same`
- `nextStep: done`

This is enough for a strong first version.

### Invalid keyword behavior

If the LLM returns an invalid keyword:

- mark the iteration as invalid
- log the failure clearly
- fail the run immediately in v1

Do not silently guess.

Deterministic routing is more important than permissive recovery.

---

## Memory Model in the New System

The memory system remains valuable, but the routed engine should be as memory-light as possible.

### Canonical persisted data should stay small

- goal
- loop definition id
- current routed step id
- phase and other compact routing state
- a compact working summary when needed
- append-only user responses
- prompt history
- raw outputs
- logs

### Canonical persisted data should avoid derived workflow views

The engine should avoid storing large markdown documents as source-of-truth when the same information already exists in JSON state or the loop definition.

That means the routed system should avoid treating these as canonical persisted memory:

- rewritten current tasks documents each iteration
- rewritten next steps documents each iteration
- repeated step instructions that already live in the loop definition
- static perspectives text that never changes after session start
- duplicated human-readable summaries that can be rendered from state on demand

### Generated artifacts are still fine

Human-readable files can still exist for observability, debugging, or operator inspection.

But they should be:

- generated from routed state and response history
- optional where possible
- excluded from canonical prompt input if the same information already exists in structured state

### User responses should be append-only and cursor-based

`respond` should append new entries into a durable response store.

The routed engine should then track a compact cursor such as `LastConsumedUserResponseId` in JSON state.

Pending responses are simply the entries with ids greater than that cursor.

This keeps the system efficient because:

- the application does not need to rewrite a full user-response memory document every time the user answers
- the next loop run only needs to inject unread responses into the prompt
- pending-response detection remains deterministic and machine-readable

### But memory should no longer be the only workflow state

The new system should add explicit machine-readable routing state.

That means:

- memory remains human-readable continuity
- loop state becomes machine-readable execution continuity

This is a major conceptual improvement.

---

## Schema Change Scope

This document should describe the scope of change required for routed loops, without locking the final field-by-field schema too early.

Exact property names, validation details, and versioning can be finalized later in the dedicated schema document.

At a high level, routed loops require schema changes in these areas:

- loop definition schema
   - enough structure to define steps, routing keywords, transitions, and safe built-in actions
- model output schema
   - enough structure to return routing decisions in a deterministic machine-readable form alongside summary data
- session continuity schema
   - enough structure to identify the active loop definition, track whether the session is active or complete, and know where to resume
- loop execution state schema
   - enough structure to track the active routed step, recent routing outcomes, unread user-response progress, and lightweight execution history
- user-response store contract
   - enough structure to support append-only responses and deterministic unread-response detection between `respond` and the next `loop` run
- artifact boundary rules
   - enough structure to clearly separate canonical state from generated prompts, raw outputs, logs, and any optional human-readable views

The important point for v1 is not exact field naming.

The important point is that routing, resume behavior, and unread user-response handling become explicit in structured state instead of being inferred from large memory documents.

---

## Minimal Memory Layout Scope

V1 should aim for the smallest on-disk memory layout that still supports deterministic routing, one-step loop execution, and `respond` between runs.

The minimal canonical layout should conceptually include only:

- one session continuity file
   - high-level session identity, selected loop definition, completion state, and resume context
- one loop execution state file
   - routed execution continuity such as current step position, phase, and unread-response progress
- one append-only user-response store
   - the durable input stream written by `respond`
- optional artifact directories
   - prompts, raw outputs, logs, and any generated operator-facing views

The routed design should explicitly avoid requiring these as canonical inputs for every iteration:

- rewritten current-tasks markdown files
- rewritten next-steps markdown files
- rewritten current-state markdown files
- full user-response narrative documents that duplicate the append-only response store
- static perspective files that can live in the loop definition itself

If operator-facing markdown files still exist, they should be treated as generated artifacts rather than required prompt-source memory.

That keeps the file layout aligned with the routing goal:

- `loop` runs one bounded routed step
- `respond` appends input without rewriting broad memory state
- the next `loop` run resumes from compact JSON state plus unread responses

---

## Proposed State Changes

The sections below describe the expected scope of state changes.

They are intentionally directional rather than final.

## Session state additions

`LoopSessionState` likely needs scope in these areas:

- active loop identity and selected routed definition
- enough resume context to know where the next `loop` run should continue
- coarse lifecycle state such as active, waiting, or complete
- high-level timestamps or metadata where useful

## Loop state additions

`LoopState` likely needs scope in these areas:

- current routed execution position
- recent routing outcome and transition continuity
- unread user-response progress
- invalid-response or retry tracking if retained in v1
- compact summaries, decisions, or question indexes only where they reduce prompt cost

### Recommended user-response contract

- the response store remains append-only
- loop state owns the canonical consumed-response cursor
- pending-response detection is derived from the response store plus that cursor
- a persisted `HasPendingUserResponses` flag is optional and should not be required if it only duplicates derivable state

### Why both session state and loop state?

- session state = durable session identity and high-level continuity
- loop state = mutable execution details for the routed engine

This matches the current architecture well.

---

## Prompt Design Changes

The current `LoopPromptBuilder` sends:

- template system prompt
- metadata
- memory documents
- response schema

The new prompt builder should become step-aware.

### Prompt should include

- shared loop instructions
- current step id and title
- current step purpose
- current step instructions
- allowed keywords for this step
- explicit rule: choose exactly one keyword
- compact working memory
- unread user responses since `LastConsumedUserResponseId`
- current routing state summary

### Prompt should avoid

- dumping unnecessary workflow explanation every time
- asking the model to infer routing from prose
- injecting large derived documents that can be regenerated from routed state
- replaying already-consumed user responses

### Example routing instruction

The prompt should say something like:

- You are currently in step `analyze_requirements`.
- You must choose exactly one keyword from this list:
  - `ask_user`
  - `continue_analysis`
  - `requirements_clear`
- Return that keyword in `selectedKeyword`.
- Do not invent new keywords.

This is the core token-saving and determinism improvement.

---

## Single-Step Invocation Model

V1 should preserve the simplest operator workflow.

Each `loop` invocation should execute exactly one routed step, persist state, and then stop.

That means the core loop experience remains:

1. run `loop`
2. WallyCode executes one bounded routed step
3. WallyCode persists routing state and artifacts
4. the process exits
5. the user either runs `loop` again or uses `respond` before the next run

This is a good fit for routed loops because it:

- keeps each provider call bounded
- keeps prompt growth under control
- gives the user a clean chance to inspect artifacts between steps
- avoids hidden multi-step behavior inside a single command invocation

### Respond behavior in the routed system

`respond` should remain part of the core lifecycle.

Recommended behavior:

1. `respond` appends a new entry into the append-only response store
2. it does not need to rewrite large memory documents
3. the next `loop` run loads the current routed step from JSON state
4. the next `loop` run compares the response cursor to the response store
5. only unread responses are injected into the prompt
6. after a successful routed step, the response cursor advances

### Waiting for user input

The `wait-for-user` action should:

- set phase to `waiting-for-user`
- persist the current step or configured next step
- end the current `loop` invocation immediately

When the user later runs `respond`, the application does not need to guess what happened.

The routed state plus response cursor tells the next `loop` invocation exactly:

- which step should resume
- whether unread user responses exist
- which responses still need to be provided to the model

---

## Built-in Actions for V1

Actions should be intentionally narrow.

Recommended v1 action set:

### 1. `wait-for-user`

Effects:

- set phase to `waiting-for-user`
- keep next step as configured
- end the current `loop` invocation after state is persisted

### 2. `mark-done`

Effects:

- set phase to `done`
- mark session done

### 3. `set-phase:<value>`

Examples:

- `set-phase:active`
- `set-phase:waiting-for-user`
- `set-phase:done`

### 4. `record-decision`

Effects:

- persist returned decisions into loop state and memory

### 5. `record-question`

Effects:

- persist returned questions into loop state and memory

### 6. `record-summary`

Effects:

- persist a compact summary for future routed steps
- avoid forcing the engine to rebuild large narrative memory files

These may overlap with current behavior, which is good. The engine should prefer compact structured state first and optional rendered artifacts second.

### What not to include in v1

- arbitrary shell commands
- arbitrary file edits from JSON actions
- arbitrary provider calls from JSON actions

Those can come later once the routing core is stable.

---

## Logging and Observability

The new system should log routing explicitly.

Each iteration log should include:

- current step id
- selected keyword
- matched transition
- actions executed
- next step id
- phase after transition
- summary
- done reason if any

This is critical for debugging JSON-defined workflows.

Without this, routed loops will be hard to trust.

---

## Forward-Only Implementation Strategy

WallyCode should treat the routed loop model as the new source of truth.

That means:

- no effort should be spent preserving the old loop schema
- no migration tooling is required
- no dual-engine support is required
- implementation decisions should optimize for the new routed model

This keeps the architecture cleaner and avoids carrying transitional complexity into the codebase.

The practical implication is simple:

1. define the new routed schema
2. implement the routed engine
3. replace the old loop template model
4. move forward

### Code cleanup requirement

This is not only a schema decision. It is also a codebase hygiene decision.

When the routed loop engine is implemented:

- legacy loop models should be removed
- old template parsing paths should be removed
- obsolete prompt-building logic should be removed
- dead state fields that only existed for the old model should be removed
- transitional adapters should not be kept around

The implementation should leave the codebase simpler than it was before the change.

---

## Suggested Implementation Plan

## Phase 1: Design and schema

1. finalize JSON schema for routed loops
2. finalize response schema with `selectedKeyword`
3. define built-in actions for v1
4. define validation rules
5. define the minimal canonical memory and response-cursor contract

## Phase 2: Engine scaffolding

6. replace the current loop template model with routed loop definition models
7. add loader/validator for routed templates
8. extend session and loop state with step/routing fields
9. add step-aware prompt builder
10. add response-cursor handling for unread user responses

## Phase 3: Runtime routing

11. update loop runner to:
   - load current step
   - parse selected keyword
   - validate transition
   - execute actions
   - persist next step
12. make one routed step per `loop` invocation the default execution model
13. update `respond` integration so it only appends responses and relies on routed state to resume cleanly
14. update logs and optional rendered artifacts

## Phase 4: Templates

15. create one routed template for requirements gathering
16. create one routed template for implementation flow
17. compare token usage and reliability across routed templates

## Phase 5: Cleanup

18. remove obsolete loop models and parsers
19. remove obsolete template files and loading paths
20. remove dead fields and transitional code
21. remove markdown memory files that are no longer canonical inputs
22. verify the final loop system only reflects the routed model

## Phase 6: Optional future work

23. nested loop invocation
24. richer action types
25. retry/recovery policies
26. richer optional generated views for operators

---

## Recommended V1 Constraints

To keep the system simple and robust, v1 should enforce:

- exactly one selected keyword per iteration
- exactly one routed step per `loop` invocation
- exact keyword matching only
- one transition per keyword
- only built-in safe actions
- no nested loops required
- fail fast on invalid routing output
- unread user responses are consumed through an append-only store plus response cursor
- no legacy schema support
- no leftover transitional code after replacement

These constraints are not limitations of vision. They are what make the first version reliable.

---

## Open Questions

These should be resolved before implementation.

1. Should routed loops reuse `LoopTemplate`, or should there be a new `RoutedLoopTemplate` type?
   - recommendation: new type or a versioned schema dedicated to routed loops

2. Should `selectedKeyword` be required even when `status = done`?
   - recommendation: yes, unless `done` itself is a valid keyword

3. Should `done` be represented as a status, a keyword, or both?
   - recommendation: keep both for now, but route primarily by keyword

4. Should actions be strings or structured objects?
   - recommendation: strings for v1 if simple, structured objects if parameters are needed immediately

5. Should step-local memory views be supported?
   - recommendation: use a compact shared memory snapshot first, and only add step-local rendered views if they reduce tokens without becoming canonical state

6. Should invalid keyword responses trigger an automatic retry with a repair prompt?
   - recommendation: no in v1; fail fast and inspect logs

---

## Recommended Direction

The strongest next move is:

1. keep `prompt` unchanged
2. replace the current loop template model with a routed state-machine engine
3. minimize canonical loop memory to goal, compact routed state, and append-only user responses
4. treat human-readable memory files as optional generated artifacts instead of prompt source-of-truth
5. keep routed loop execution single-step per invocation in v1
6. keep `respond` append-only and make resume behavior cursor-driven
7. define loops in JSON
8. route by explicit LLM keywords
9. keep actions safe and internal
10. remove obsolete loop code and memory-heavy prompt inputs as part of the implementation
11. ship a minimal deterministic v1 before adding nested loops or arbitrary actions

This gives WallyCode a much stronger architecture:

- prompt remains simple
- loop becomes programmable
- memory remains durable without carrying unnecessary derived state
- routing becomes explicit
- token usage can improve
- workflows become easier to author and reason about
- the codebase stays cleaner instead of accumulating compatibility debt

---

## Proposed Next Deliverables

After this document, the next concrete deliverables should be:

1. `docs/loop-routing-schema-v1.md`
   - exact JSON schema proposal

2. `docs/loop-routing-examples.md`
   - example routed loops
   - requirements loop
   - implementation loop
   - review/fix loop

3. implementation plan issue/task list
   - model changes
   - parser changes
   - runner changes
   - cleanup removals

That is the right sequence before code changes begin.
