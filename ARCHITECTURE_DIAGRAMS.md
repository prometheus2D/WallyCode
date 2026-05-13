# WallyCode Architecture Diagrams

WallyCode is centered on a deterministic workflow orchestrator. LLMs provide judgment, routing suggestions, and perspective, but the runtime owns workflow definitions, session state, memory retention, executor selection, transition resolution, and snapshots.

This document is layered by audience. Start near the top for business context and user workflows; move down for authoring and runtime details.

## Executive Overview

This view is for directors, sponsors, and business users who need to understand what WallyCode controls and why workflow definitions matter.

```mermaid
flowchart LR
    User[User goal or question] --> Command[WallyCode command]
    Command --> Workflow[Named workflow definition]
    Workflow --> Orchestrator[Deterministic orchestrator]
    Orchestrator --> Provider[LLM provider]
    Orchestrator --> State[Session state and snapshots]
    Provider --> Response[Structured response]
    Response --> Orchestrator
    Orchestrator --> Outcome[Answer, action result, tasks, or blocker]
    State --> Audit[Recoverable execution trail]

    Workflow --> Guardrails[Allowed steps, transitions, and memory]
    Guardrails --> Orchestrator
```

Key point: the LLM is not running an open-ended process. WallyCode constrains each run through a named workflow, declared steps, allowed transitions, and declared memory contracts.

## User Workflow Diagrams

This view helps users pick the right workflow for the job.

```mermaid
flowchart TD
    Start[What do you need?]
    Start --> Question{Need an answer only?}
    Question -- yes --> Ask[ask workflow]
    Ask --> AskDone[One-shot answer]

    Question -- no --> Change{Need a single action?}
    Change -- yes --> Act[act workflow]
    Act --> ActDone[One-shot action result]

    Change -- no --> NeedClarify{Need clarification first?}
    NeedClarify -- yes --> Requirements[requirements workflow]
    Requirements --> ReqFlow[Clarify requirements to produce tasks to execute tasks]

    NeedClarify -- no --> Tasks[tasks workflow]
    Tasks --> TaskFlow[Produce tasks to execute tasks]
```

The built-in workflow paths are intentionally different sizes:

```mermaid
flowchart LR
    AskStart[ask] --> AskStep[ask step]
    AskStep --> AskStop[stop]

    ActStart[act] --> ActStep[act step]
    ActStep --> ActStop[stop]

    ReqStart[requirements] --> Collect[collect_requirements]
    Collect --> ProduceA[produce_tasks]
    ProduceA --> ExecuteA[execute_tasks]
    ExecuteA --> ReqStop[stop]

    TaskStart[tasks] --> ProduceB[produce_tasks]
    ProduceB --> ExecuteB[execute_tasks]
    ExecuteB --> TaskStop[stop]
```

`ask` and `act` are one-shot workflows. The multi-step workflows exist for work that benefits from clarification, task planning, and execution handoffs.

## Workflow Authoring Model

This view is for users and developers who want to understand or extend the JSON loadables.

```mermaid
flowchart TB
    DefinitionFile[Loadables/Definitions/*.json] --> Definition[Workflow definition]
    Definition --> DefinitionId[id]
    Definition --> DefinitionInstructions[instructions]
    Definition --> StartStep[startStepName]
    Definition --> AllowedSteps[stepIds]

    StepFile[Loadables/Steps/*.json] --> Step[Workflow step]
    Step --> ExecutionKind[executionKind]
    Step --> StepInstructions[instructions]
    Step --> ReadsMemory[readsMemory]
    Step --> WritesMemory[writesMemory]
    Step --> TransitionIds[transitionIds]

    TransitionFile[Loadables/Transitions/*.json] --> Transition[Workflow transition]
    Transition --> Selection[selection]
    Transition --> Target[targetStepName]
    Transition --> Status[status]
    Transition --> Stops[stopsInvocation]
    Transition --> Guard[optional guard]

    AllowedSteps --> Compile[Compile workflow]
    StartStep --> Compile
    Step --> Compile
    Transition --> Compile
    Compile --> RuntimeDefinition[Runtime WorkflowDefinition]
```

The definition selects which shared steps are in the workflow. Each step selects which shared transitions are allowed. When compiled, transitions that target steps outside the workflow are filtered out.

## Runtime Orchestration

This sequence shows one normal workflow iteration from command invocation to saved session state.

```mermaid
sequenceDiagram
    participant CLI as CLI command
    participant Handler as Command handler
    participant Catalog as Workflow catalog
    participant Session as Session store
    participant Orchestrator as WorkflowOrchestrator
    participant Executor as Step executor
    participant Provider as LLM provider or script
    participant Resolver as TransitionResolver

    CLI->>Handler: run, ask, act, step, resume, or respond
    Handler->>Catalog: load workflow definition
    Handler->>Session: load or start session
    Handler->>Orchestrator: run active workflow
    Orchestrator->>Session: read active step and memory
    Orchestrator->>Executor: execute active step
    Executor->>Provider: prompt or script execution
    Provider-->>Executor: selectedStep, summary, memory
    Executor-->>Orchestrator: StepExecutionResult
    Orchestrator->>Orchestrator: filter memory through writesMemory
    Orchestrator->>Resolver: resolve transition
    Resolver-->>Orchestrator: next step, status, stopsInvocation
    Orchestrator->>Session: retain next memory, save session, save snapshot
    Orchestrator-->>Handler: IterationResult
```

A more detailed control-flow view of the same iteration:

```mermaid
flowchart TD
    Definition[Load named WorkflowDefinition] --> Load[Load session.json]
    Load --> Guards{Session valid and active?}
    Guards -- no --> Throw[Throw before mutation]
    Guards -- yes --> Step[Resolve active WorkflowStep]
    Step --> Executor[Select step executor by executionKind]

    Executor --> Provider{provider step?}
    Provider -- yes --> Prompt[Build scoped prompt<br/>goal, instructions, declared memory reads, transitions]
    Prompt --> LLM[Call ILlmProvider]
    LLM --> Parse[Parse selectedStep, summary, memory]

    Executor --> Script{script step?}
    Script -- yes --> RunScript[Run scriptPath with timeout]
    RunScript --> ScriptResult[Return summary and memory updates]

    Parse --> Contract[Filter memory updates through writesMemory]
    ScriptResult --> Contract
    Contract --> Resolve[TransitionResolver]
    Resolve --> Guarded{Any explicit guard matches?}
    Guarded -- yes --> GuardRoute[Use guarded transition]
    Guarded -- no --> ModelRoute[Use selectedStep from executor]

    GuardRoute --> Handoff[Enforce derived handoff memory]
    ModelRoute --> Handoff
    Handoff --> Retain[Retain current updates and next-step reads]
    Retain --> Save[Save session.json]
    Save --> Snapshot[Save sessions/session-000N.json]
    Snapshot --> Result[Return IterationResult]

    Executor --> Failure[Exception]
    Resolve --> Failure
    Contract --> Failure
    Failure --> ErrorState[Set status error, selectedStep error, summary message]
    ErrorState --> SaveError[Save session and snapshot]
    SaveError --> Rethrow[Rethrow for CLI failure]
```

## Memory And Session Lifecycle

This view explains how WallyCode avoids stale context while still carrying forward the memory the next step needs.

```mermaid
flowchart TD
    CurrentSession[(Current session memory)] --> Reads[Inject only active step readsMemory]
    Reads --> Prompt[Provider prompt]
    Prompt --> Response[Provider returns selectedStep, summary, memory]
    Response --> Filter[Filter memory through active step writesMemory]
    Filter --> Resolve[Resolve next step]
    Resolve --> NextReads[Look at next step readsMemory]
    NextReads --> RetainExisting[Carry existing values needed by next step]
    Filter --> RetainUpdates[Carry current non-null updates]
    RetainExisting --> Replace[Replace session memory]
    RetainUpdates --> Replace
    Replace --> FreshMemory[(Fresh session memory)]
    Replace --> Clear[Clear unrelated previous keys]
```

Session status is separate from memory. Status determines whether the workflow can continue, is waiting on the user, or is terminal.

```mermaid
stateDiagram-v2
    [*] --> active: start session
    active --> active: continue or route to another step
    active --> blocked: ask_user
    blocked --> active: respond
    active --> completed: stop
    active --> error: error or exception
    blocked --> error: error or exception
    completed --> [*]
    error --> [*]
```

Memory is a handoff packet, not a permanent notebook. If a later step still needs context, that context must either be declared in the next step's `readsMemory` or written again by the current step.

## Developer Responsibility Split

This view is for maintainers who need to know which component owns each part of execution.

```mermaid
flowchart LR
    Command[Command handlers] --> Catalog[WorkflowDefinition loader]
    Catalog --> Definitions[Loadables/Definitions]
    Catalog --> Steps[Loadables/Steps]
    Catalog --> Transitions[Loadables/Transitions]

    Command --> Orchestrator[WorkflowOrchestrator]
    Catalog --> Orchestrator
    Orchestrator --> Executors[Step executors]
    Executors --> ProviderStep[ProviderStepExecutor]
    Executors --> ScriptStep[ScriptStepExecutor]
    Orchestrator --> Resolver[TransitionResolver]
    Resolver --> Guards[Explicit guards]
    Resolver --> Handoffs[Derived memory handoffs]
    Resolver --> Selection[Executor selectedStep]
    Orchestrator --> Session[(session.json)]
    Orchestrator --> Snapshots[(sessions/session-000N.json)]
    Session --> PromptMemory[Declared readsMemory injected into later prompts]
```

## Key Invariants

- `WorkflowOrchestrator` owns session mutation and snapshots.
- Workflow definitions own workflow-level instructions, start step, and allowed step IDs. Compiled workflows expose only transitions whose targets stay inside those allowed step IDs.
- Step executors produce `StepExecutionResult`; they do not directly mutate the session.
- Provider steps call the LLM and parse strict JSON: `selectedStep`, `summary`, and optional `memory`.
- Script steps are deterministic executors for future verification, build, and local command steps.
- `writesMemory` is enforced by filtering provider or script memory updates before persistence.
- After each successful iteration, session memory is replaced with the current non-null memory updates plus existing keys declared by the next step's `readsMemory`; unrelated previous memory is cleared.
- Explicit guarded transitions are evaluated before model-selected transitions.
- Target-step handoff memory is derived from `writesMemory` and `readsMemory`, so basic artifact readiness does not need custom guard JSON.
- `continue`, route transitions, `ask_user`, `stop`, and `error` are the externally visible routing vocabulary.
