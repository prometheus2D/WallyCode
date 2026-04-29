# WallyCode Architecture Diagrams

This document captures Mermaid diagrams describing how the routed loop engine in
`WallyCode.Console` is structured. The diagrams are derived from the live source
under `WallyCode.Console/Routing` and `WallyCode.Console/Commands`.

- Diagram 1 zooms into a single logical-unit iteration (`RoutedRunner.RunOnceAsync`).
- Diagram 2 covers the full CLI-to-loop workflow, including session lifecycle,
  routing-catalog loading, and the user feedback loop via `respond` / `resume`.

---

## 1. Lifecycle of a single logical unit iteration

Shows every optional/conditional input feeding one `RoutedRunner.RunOnceAsync`
call, how the prompt is assembled, how the provider's response is parsed and
validated, and how the session is mutated and persisted.

```mermaid
flowchart TD
    subgraph Precursors["Precursors (resolved once at RoutedRunner construction or per-iteration)"]
        direction TB
        S[(session.json<br/>RoutedSession.Load at iteration start)]
        subgraph DefResolved["RoutingDefinition (already resolved by RoutingCatalog)"]
            D1[LogicalUnit for session.ActiveUnitName<br/>• Name<br/>• Instructions (may be empty)<br/>• AllowedKeywords<br/>• KeywordOptions descriptions<br/>• Transitions keyword → nextUnit]
        end
        subgraph GPResolve["Global prompt resolution (ctor: LoadGlobalPrompt)"]
            GPA{"parent(sessionRoot) exists<br/>and is a directory?"}
            GPA -- no --> GPEmpty["_globalPrompt = '' (skipped)"]
            GPA -- yes --> GPLoad[ProjectSettings.Load projectRoot]
            GPLoad --> GPVal{GlobalPrompt<br/>non-empty after trim?}
            GPVal -- no --> GPEmpty
            GPVal -- yes --> GPUse["_globalPrompt = trimmed value"]
        end
        LoggerOpt["AppLogger? _logger<br/>(null when runner is instantiated without one)"]
    end

    S --> Guard1{"session.DefinitionName<br/>== _definition.Name?"}
    Guard1 -- no --> Throw1[["throw InvalidOperationException<br/>'Session is on definition X but Y was supplied'<br/>bubbles to Program.RunAsync catch → exit 1"]]
    Guard1 -- yes --> Guard2{"session.Status in<br/>Completed or Error?"}
    Guard2 -- yes --> Throw2[["throw InvalidOperationException<br/>'Session is completed/error; nothing to run'"]]
    Guard2 -- no --> Resolve

    Resolve[_definition.GetUnit session.ActiveUnitName]
    D1 --> Resolve

    Resolve --> Build
    subgraph BuildBlock["BuildPrompt (StringBuilder, conditional sections)"]
        direction TB
        B0["Goal: session.Goal<br/>Active unit: unit.Name<br/>(always present)"]
        B1{"globalPrompt<br/>non-empty?"}
        B1 -- yes --> B1Y["append 'Global prompt:'<br/>then the prompt text"]
        B1 -- no --> B1N[skip section]
        B2{"unit.Instructions<br/>non-whitespace?"}
        B2 -- yes --> B2Y["append 'Instructions: ...'"]
        B2 -- no --> B2N[skip section]
        B3["append 'Keyword options:'<br/>for each k in unit.AllowedKeywords:<br/>  '  - k: unit.DescribeKeyword(k)'<br/>(description falls back to '' if missing)"]
        B4{"session.PendingResponses.Count > 0?"}
        B4 -- yes --> B4Y["append 'User responses since last run:'<br/>then '  - r' for each"]
        B4 -- no --> B4N[skip section]
        B5["always append trailer:<br/>• 'Choose the keyword that best matches ...'<br/>• 'If an unrecoverable problem occurred, select [ERROR]'<br/>• strict JSON contract: selectedKeyword and summary<br/>• 'selectedKeyword must be one of the allowed keywords ... Output JSON only.'<br/>• 'When selecting [ERROR], put reason in summary.'"]
        B0 --> B1 --> B2 --> B3 --> B4 --> B5
        B1Y --- B2
        B1N --- B2
        B2Y --- B3
        B2N --- B3
        B4Y --- B5
        B4N --- B5
    end
    GPEmpty -. feeds .-> B1
    GPUse -. feeds .-> B1

    Build[prompt string ready]
    B5 --> Build

    Build --> Log1
    Log1{"_logger != null?"}
    Log1 -- yes --> Log1Y[/"_logger.LogExchange 'OUT',<br/>'iteration N+1 prompt (unit)', prompt"/]
    Log1 -- no --> Log1N[no-op]
    Log1Y --> ProviderCall
    Log1N --> ProviderCall
    LoggerOpt -. guards .-> Log1

    ProviderCall["_provider.ExecuteAsync with CopilotRequest:<br/>Prompt = prompt,<br/>Model = session.Model (may be null → provider default),<br/>SourcePath = session.SourcePath,<br/>cancellationToken"]

    ProviderCall --> ProviderOutcome{"outcome?"}

    ProviderOutcome -- "OperationCanceledException" --> Cancel[["rethrow OCE<br/>(caught by Program → 'Cancelled.' → exit 2)<br/>session file NOT mutated"]]
    ProviderOutcome -- "any other exception" --> Funnel
    ProviderOutcome -- "raw string" --> Raw[(raw output text)]

    Raw --> Log2
    Log2{"_logger != null?"}
    Log2 -- yes --> Log2Y[/"_logger.LogExchange 'IN',<br/>'iteration N+1 response (unit)', rawOutput"/]
    Log2 -- no --> Log2N[no-op]
    Log2Y --> Parse
    Log2N --> Parse

    subgraph ParseBlock["ParseOutput"]
        direction TB
        P0{"rawOutput<br/>null/whitespace?"}
        P0 -- yes --> P0T[["throw 'Provider output was empty'"]]
        P0 -- no --> P1["trim"]
        P1 --> P2{"starts with triple-backtick fence?"}
        P2 -- yes --> P2Y["drop first line after opening fence<br/>drop trailing fence if present<br/>trim again"]
        P2 -- no --> P2N[leave as-is]
        P2Y --> P3
        P2N --> P3
        P3{"first '{' and last '}' found<br/>with first less than last?"}
        P3 -- no --> P3T[["throw 'No JSON object found in provider output'"]]
        P3 -- yes --> P4[JsonDocument.Parse substring]
        P4 --> P5{"root has 'selectedKeyword'<br/>as string?"}
        P5 -- no --> P5T[["throw 'Provider output is missing selectedKeyword string'"]]
        P5 -- yes --> P6["keyword = value.Trim()<br/>summary = root.summary (string) else ''"]
    end
    Parse[Parse]
    Parse --> P0
    P0T --> Funnel
    P3T --> Funnel
    P5T --> Funnel
    P6 --> Validate

    Validate{"keyword ∈<br/>unit.AllowedKeywords?"}
    Validate -- no --> ValidateThrow[["throw 'keyword is not allowed for unit X'"]]
    ValidateThrow --> Funnel
    Validate -- yes --> Apply

    subgraph ApplyBlock["ApplyKeyword"]
        direction TB
        A1{"unit.Transitions<br/>contains keyword?"}
        A1 -- yes --> A1Y["(nextUnit = transitions[keyword],<br/>status = Active, stops = false)"]
        A1 -- no --> A2{"which built-in?"}
        A2 -- "[CONTINUE]" --> A2C["(nextUnit = unit.Name,<br/>status = Active, stops = false)"]
        A2 -- "[ASK_USER]" --> A2K["(nextUnit = unit.Name,<br/>status = Blocked, stops = true)"]
        A2 -- "[DONE]" --> A2D["(nextUnit = unit.Name,<br/>status = Completed, stops = true)"]
        A2 -- "[ERROR]" --> A2E["(nextUnit = unit.Name,<br/>status = Error, stops = true)"]
        A2 -- "other (unreachable once Validate passed, but guarded)" --> A2X[["throw 'Keyword X has no transition and is not a built-in'"]]
    end
    Apply[Apply]
    Apply --> A1
    A2X --> Funnel

    A1Y --> Mutate
    A2C --> Mutate
    A2K --> Mutate
    A2D --> Mutate
    A2E --> Mutate

    Mutate["Mutate session (happy path):<br/>• IterationCount++<br/>• LastSelectedKeyword = keyword<br/>• LastSummary = summary<br/>• ActiveUnitName = nextUnit<br/>• Status = status (Active / Blocked / Completed / Error from model-chosen [ERROR])<br/>• PendingResponses.Clear()"]
    Mutate --> SaveOk[(session.Save → session.json<br/>File.WriteAllText; IOException bubbles)]
    SaveOk --> Result[["return IterationResult:<br/>IterationNumber, SelectedKeyword, Summary,<br/>ActiveUnitName = nextUnit, Status, StopsInvocation = stops"]]

    Funnel["Failure funnel<br/>catch (Exception ex) when (ex is not OperationCanceledException):<br/>• IterationCount++<br/>• LastSelectedKeyword = '[ERROR]'<br/>• LastSummary = ex.Message<br/>• Status = Error<br/>• ActiveUnitName unchanged<br/>• PendingResponses.Clear()"]
    Funnel --> SaveErr[(session.Save → session.json)]
    SaveErr --> Rethrow[["throw; (rethrow original)<br/>→ caught by Program → exit 1"]]
```

### Key invariants

- **Optional prompt sections** (each is *only* appended when present and non-whitespace): `Global prompt`, per-unit `Instructions`, `User responses since last run`. `Goal`, `Active unit`, `Keyword options:` list, and the JSON-contract trailer are always present.
- **Global prompt resolution** happens once, in the `RoutedRunner` constructor, by walking `Directory.GetParent(sessionRoot)`. If that directory does not exist, or `wallycode.json` does not define `globalPrompt`, the runner simply treats it as empty and skips the section for every iteration.
- **Logger is optional** (`AppLogger?`). The two `LogExchange` points (`OUT` prompt, `IN` raw output) are no-ops when it is null — no ambient side effects.
- **Provider model** resolves at the `CopilotRequest` boundary: `session.Model` may be null, in which case each `ILlmProvider` implementation is responsible for its own default (`DefaultModel`).
- **Parsing** always trims, strips one layer of triple-backtick fences if present, then extracts the first balanced `{…}` substring before `JsonDocument.Parse`. `summary` is optional (defaults to `""`); `selectedKeyword` is required and trimmed.
- **Keyword validation** enforces that `selectedKeyword` is in `unit.AllowedKeywords`. Only after that does `ApplyKeyword` check transitions first, then fall back to the four built-ins.
- **Happy-path mutation and save happen exactly once** per successful iteration; `PendingResponses` is consumed and cleared.
- **Failure funnel:** any exception thrown inside the risky middle (`GetUnit` → `BuildPrompt`/logger → provider call → `ParseOutput` → keyword validation → `ApplyKeyword`) is caught, the session is stably parked at `Status = Error` with `LastSelectedKeyword = "[ERROR]"` and `LastSummary = ex.Message`, and the exception is rethrown so the CLI still exits non-zero.
- **Not funnelled (intentional):** guard-clause throws (`Load` failure, definition mismatch, already-terminal status) — they pre-date any per-iteration state — and `OperationCanceledException` — a clean user cancel leaves the session untouched so it can be resumed.

---

## 2. End-to-end workflow (CLI → routed session loop)

Shows how the CLI verbs (`loop`, `resume`, `respond`, `ask`, `act`) cooperate
with `wallycode.json`, the routing catalog, the provider registry, and the
per-iteration loop above. Each optional input/fallback is made explicit.

```mermaid
flowchart TD
    User([User CLI invocation]) --> Program[Program.Main → RunAsync<br/>• build CancellationTokenSource<br/>• hook Console.CancelKeyPress → Cancel]
    Program --> InvLog{"args[0] supports invocation logging<br/>AND --log flag present?"}
    InvLog -- yes --> InvLogY["ConfigureInvocationLogging:<br/>resolve projectRoot from --source,<br/>resolve runtimeRoot from --memory-root,<br/>AppLogger.ConfigureLogging + LogCommand"]
    InvLog -- no --> InvLogN[skip]
    InvLogY --> Parser
    InvLogN --> Parser
    Parser["CommandLineParser.ParseArguments<br/>case-insensitive, AutoHelp, AutoVersion"]

    Parser -->|loop| Loop[LoopCommandHandler.ExecuteAsync]
    Parser -->|resume| Resume[ResumeCommandHandler.ExecuteAsync<br/>→ delegates to LoopCommandHandler]
    Parser -->|respond| Respond[RespondCommandHandler.ExecuteAsync]
    Parser -->|ask / act| Ask["AskCommandOptions / ActCommandOptions<br/>.ToLoopOptions() → LoopCommandHandler"]
    Parser -->|provider| Prov[ProviderCommandHandler]
    Parser -->|logging| Logg[LoggingCommandHandler]
    Parser -->|shell| Shell[ShellCommandHandler]
    Parser -->|setup| Setup[SetupCommandHandler]
    Parser -->|help/version| Help[[exit 0]]
    Parser -->|parse errors| ParseErr[[exit 1]]

    subgraph Shared["Shared resolution (loop/resume/respond/ask/act)"]
        direction TB
        PR1["ProjectSettings.ResolveProjectRoot(options.SourcePath)<br/>= CurrentDirectory if null<br/>else Path.GetFullPath(SourcePath)<br/>throws if the directory doesn't exist"]
        PR2["ProjectSettings.ResolveRuntimeRoot(projectRoot, MemoryRoot)<br/>= MemoryRoot (abs) if provided<br/>else projectRoot + '.wallycode'"]
        PR3["ProjectSettings.Load(projectRoot)<br/>reads wallycode.json or defaults:<br/>• Provider = gh-copilot-cli if missing<br/>• Model may be null<br/>• GlobalPrompt may be null<br/>• Logging (Enabled/Verbose)<br/>• ProviderCatalog"]
        PR4["LoggingMode =<br/>(options.Log || settings.Logging.Enabled,<br/> options.Verbose || settings.Logging.Verbose)<br/>→ AppLogger.ConfigureLogging(sessionRoot, mode)"]
        PR1 --> PR2 --> PR3 --> PR4
    end

    Loop --> Shared
    Ask --> Shared
    Respond --> Shared
    Resume --> PR1R[ResumeCommandHandler uses ResolveProjectRoot + ResolveRuntimeRoot only<br/>then hands off to LoopCommandHandler which re-runs Shared]
    PR1R --> Resume2

    %% Respond path
    Respond --> RespEmpty{options.Response<br/>non-empty after trim?}
    RespEmpty -- no --> RespErr0[[throw 'A non-empty response is required']]
    RespEmpty -- yes --> RespExists{RoutedSession.Exists<br/>at sessionRoot?}
    RespExists -- no --> RespErr1[[throw 'No active session at ...']]
    RespExists -- yes --> RespLoad[RoutedSession.Load]
    RespLoad --> RespMut["append trimmed response to PendingResponses<br/>if Status == Blocked → flip to Active"]
    RespMut --> RespSave[(session.Save)]
    RespSave --> RespLog["LogAction + LogExchange 'USER'<br/>Success 'Response saved'"]

    %% Resume path (after guard)
    Resume2{"RoutedSession.Exists?"}
    Resume2 -- no --> ResErr0[["throw 'No resumable session. Start one with loop goal'"]]
    Resume2 -- yes --> ResLoad[RoutedSession.Load]
    ResLoad --> ResStatus{session.Status?}
    ResStatus -- Blocked --> ResBlocked[[throw 'Session is waiting for user input. Use respond before resume.']]
    ResStatus -- Completed/Error --> ResTerm[[throw 'Session is terminal with status X and cannot be resumed']]
    ResStatus -- Active --> ResForward["options.ToLoopOptions()<br/>→ LoopCommandHandler.ExecuteAsync"]
    ResForward --> Loop

    %% Loop path
    Loop --> LoopSteps{options.GetEffectiveSteps > 0?}
    LoopSteps -- no --> StepsErr[[throw 'Steps must be greater than zero']]
    LoopSteps -- yes --> LoopExists{RoutedSession.Exists<br/>at sessionRoot?}

    LoopExists -- yes --> LoadExisting[RoutedSession.Load]
    LoadExisting --> DefCheck{"options.Definition (if supplied)<br/>== session.DefinitionName?"}

    DefCheck -- no --> DefErr[[throw 'Active session uses definition X. Use --memory-root for a different one.']]
    DefCheck -- yes --> Terminal{"IsTerminal(session.Status)<br/>(Completed or Error)?"}

    Terminal -- yes --> Archive["RoutedSession.ArchiveCompletedSession<br/>• creates archive/session-YYYYMMDD-HHMMSS<br/>• collision suffix -1, -2, ... if exists<br/>• moves every top-level entry EXCEPT 'archive'<br/>  into the archive dir (Directory.Move or File.Move)"]
    Archive --> ArchLog[Info + optional 'previous error' warning]
    ArchLog --> GoalGiven{"options.Goal<br/>supplied and non-empty?"}

    GoalGiven -- no --> Done0[[return 0<br/>session reported as already-terminal]]
    GoalGiven -- yes --> StartNew

    Terminal -- no --> ActiveCheck{status?}
    ActiveCheck -- Blocked --> WarnBlocked[[Warning 'Session is blocked. Use respond to provide input.'<br/>return 0]]
    ActiveCheck -- Active --> ResolveRun

    subgraph ResolveRun["Resume-existing-active path"]
        direction TB
        RRDef["definition =<br/>RoutingDefinition.LoadByName(session.DefinitionName)"]
        RRProv["provider =<br/>ProviderRegistry.Get(session.ProviderName)"]
        RRReady["await provider.EnsureReadyAsync(ct)<br/>throws if provider not ready"]
        RRDef --> RRProv --> RRReady
    end
    ResolveRun --> RunnerLoop

    LoopExists -- no --> NeedGoal{options.Goal<br/>non-empty?}
    NeedGoal -- no --> NoGoal[["throw 'No active session. Start one with loop goal optional --definition name'"]]
    NeedGoal -- yes --> StartNew

    subgraph StartNew["New-session path"]
        direction TB
        SNProv["providerName =<br/>options.Provider ?? settings.Provider"]
        SNProv --> SNLook["provider = providerRegistry.Get(providerName)"]
        SNLook --> SNModel["model =<br/>options.Model<br/>?? settings.Model<br/>?? provider.DefaultModel"]
        SNModel --> SNDef["definition =<br/>RoutingDefinition.LoadByName(options.Definition ?? 'requirements')"]
        SNDef --> SNStart["session = RoutedSession.Start(<br/>  definition, goal, provider.Name, model,<br/>  sourcePath set to projectRoot)"]
        SNStart --> SNSave[(session.Save — BEFORE provider.EnsureReadyAsync)]
        SNSave --> SNReady["await provider.EnsureReadyAsync(ct)<br/>NOTE: if this throws, the just-saved Active session<br/>remains on disk and will be reloaded next run"]
    end
    StartNew --> RunnerLoop

    %% Catalog resolution
    subgraph Catalog["RoutingCatalog.LoadFromBaseDirectory (one-shot)"]
        direction TB
        CatRoot[["AppContext.BaseDirectory + 'Routing/'"]]
        CatRoot --> CatKw["Keywords/*.json → KeywordDefinition (id, description)"]
        CatRoot --> CatUnits["Units/*.json → SharedLogicalUnitDefinition (id, name, ...)"]
        CatRoot --> CatDefs["Definitions/*.json → RoutingDefinition (name, startUnitName, units, unitRefs)"]
        CatKw --> CatResolve
        CatUnits --> CatResolve
        CatDefs --> CatResolve
        CatResolve["ResolveAndValidate:<br/>• inline units: ApplyKeywordDefinitions (fill missing descriptions from shared keywords) + ValidateShape<br/>• unitRefs: clone shared unit, apply name override, prompt addon, keyword-option overrides, transition overrides, executionKind/scriptPath overrides<br/>• check duplicate unit names<br/>• validate startUnitName is declared<br/>• validate every transition target is a known unit (local or qualified name/unit)"]
    end
    CatResolve -. supplies .-> ResolveRun
    CatResolve -. supplies .-> StartNew

    %% Runner
    RunnerLoop["new RoutedRunner(provider, definition, sessionRoot, logger)<br/>→ RunAsync(effectiveSteps, ct)"]
    RunnerLoop --> Iter[/"RunOnceAsync (see Diagram 1)"/]
    Iter --> IterOutcome{outcome?}
    IterOutcome -- "throws (non-cancel)" --> IterThrow[["session already parked at Status=Error by funnel<br/>exception propagates → Program catch → exit 1"]]
    IterOutcome -- "OperationCanceledException" --> IterCancel[["Program catch → Warning 'Cancelled.' → exit 2<br/>(session state preserved)"]]
    IterOutcome -- "IterationResult" --> StopQ{result.StopsInvocation?}
    StopQ -- yes --> Report
    StopQ -- no --> StepsLeft{more steps left?}
    StepsLeft -- yes --> CancelCheck{"ct.IsCancellationRequested?"}
    CancelCheck -- no --> Iter
    CancelCheck -- yes --> IterCancel
    StepsLeft -- no --> Report

    Report["Per-iteration logging (from accumulated results):<br/>Section 'Iteration N', Info: keyword, summary, next unit, status"]
    Report --> FinalStatus{last result.Status?}
    FinalStatus -- Error --> WarnErr["Warning 'Error: summary'<br/>Success 'Run complete after N iteration(s).'<br/>return 0 (Program still exits 0 because runner ended cleanly with a model-chosen [ERROR])"]
    FinalStatus -- Blocked --> WaitUser([User runs 'respond' → back to Respond path])
    FinalStatus -- Completed --> WaitArchive([Next 'loop' invocation will archive → Archive node])
    FinalStatus -- Active --> ReadyResume([User runs 'resume' or another 'loop' → back to Loop path])

    WaitUser --> Respond
    ReadyResume --> Resume
    WaitArchive --> Loop
