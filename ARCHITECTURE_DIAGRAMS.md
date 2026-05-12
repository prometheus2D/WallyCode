# WallyCode Architecture Diagrams

WallyCode is centered on a deterministic workflow orchestrator. LLMs still provide judgment, routing suggestions, and perspective, but the runtime owns workflow definitions, session state, memory persistence, executor selection, guarded transition resolution, and snapshots.

## 1. One Orchestrated Iteration

```mermaid
flowchart TD
    Definition[Load named WorkflowDefinition] --> Load[Load session.json]
    Load --> Guards{Session valid and active?}
    Guards -- no --> Throw[Throw before mutation]
    Guards -- yes --> Step[Resolve active WorkflowStep]
    Step --> Executor[Select step executor by executionKind]

    Executor --> Provider{provider step?}
    Provider -- yes --> Prompt[Build token-scoped prompt<br/>goal, workflow instructions, step instructions, declared memory reads, transitions]
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
    Handoff --> Mutate[Update session memory, step, status, summary]
    Mutate --> Save[Save session.json]
    Save --> Snapshot[Save sessions/session-000N.json]
    Snapshot --> Result[Return IterationResult]

    Executor --> Failure[Exception]
    Resolve --> Failure
    Contract --> Failure
    Failure --> ErrorState[Set status error, selectedStep error, summary message]
    ErrorState --> SaveError[Save session and snapshot]
    SaveError --> Rethrow[Rethrow for CLI failure]
```

## 2. Responsibility Split

```mermaid
flowchart LR
    CLI[run / step / ask / act / resume] --> Handler[Command handlers]
    Handler --> Catalog[WorkflowCatalog]
    Catalog --> Definitions[Workflow/Definitions/*.json]
    Catalog --> Steps[Workflow/Steps/*.json]
    Catalog --> Transitions[Workflow/Transitions/*.json]
    Handler --> Orchestrator[WorkflowOrchestrator]
    Catalog --> Orchestrator
    Orchestrator --> Executors[Step executors]
    Executors --> ProviderStep[ProviderStepExecutor]
    Executors --> ScriptStep[ScriptStepExecutor]
    Orchestrator --> Resolver[TransitionResolver]
    Resolver --> Guards[Explicit guards]
    Resolver --> Handoffs[Derived memory handoffs]
    Resolver --> Selection[LLM selectedStep]
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
- Explicit guarded transitions are evaluated before model-selected transitions.
- Target-step handoff memory is derived from `writesMemory` and `readsMemory`, so basic artifact readiness does not need custom guard JSON.
- `continue`, route transitions, `ask_user`, `stop`, and `error` are the externally visible routing vocabulary.
