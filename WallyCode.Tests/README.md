# WallyCode.Tests

## ?? Testing safety policy � read this first

**Tests must never invoke real LLM providers, real `gh` / `copilot` CLIs, or any
external service that costs money, consumes API quota, mutates a real GitHub
account, or hits the network for billable work.**

Concretely, this means:

1. **Always use `MockLlmProvider`.** Never instantiate `GhCopilotCliProvider`
   or any other real `ILlmProvider` implementation in a test.
2. **Always inject the mock.** For handler tests, construct
   `new ProviderRegistry([new MockLlmProvider(...)])` and pass it to the
   handler. For end-to-end tests, use `CliHarness.Create(...)` which passes
   the mock registry into `Program.RunAsync` via its optional
   `providerRegistry` parameter.
3. **Never call `ProviderRegistry.Create(logger)` from a test.** That static
   factory loads `Providers/*.json` from disk and constructs real
   `gh-copilot-cli` providers.
4. **Never run `gh`, `copilot`, `git` (against remotes), `dotnet nuget push`,
   `curl`, or anything similar from a test.**

### Defense-in-depth tripwire

`WallyCode.Tests/TestInfrastructure/ForbiddenProductionTypeReferenceTests.cs`
is a static-analysis test that lives entirely in the test project (production
code has no awareness of testing). It runs as part of the normal test suite
and fails the build if:

  * the test assembly's metadata `TypeRef` table contains a reference to a
    forbidden production type (`GhProcess` and its nested `GhResult`), or
  * any test method's IL contains a call to `ProviderRegistry.Create(...)`.

`GhProcess` is the single chokepoint that launches `gh` / `copilot` child
processes; every dangerous method on `GhCopilotCliProvider` routes through
it, so banning references to `GhProcess` is both necessary and sufficient.
Constructing a `GhCopilotCliProvider` (without calling its execute methods)
is allowed for shape-only tests like `ProviderRegistryTests`.

If this test ever fails, do **not** add the offending type to its allowlist.
Fix the test that reached for real infrastructure, and use `MockLlmProvider`
+ `CliHarness` instead.

---

Tests are organized by **layer of abstraction**, from narrow to broad. Pick the
narrowest layer that can express the assertion you need � broader layers are
more expensive to write and slower to fail-diagnose.

## Layers

### `Workflow/` � workflow & session unit tests
Pure unit tests for the workflow engine: `Runner`, `WorkflowDefinition`
validation, `Session` save/load/archive. No CLI, no command handlers.
Use when you are testing keyword routing, transition logic, prompt
construction, or persistence shape.

### `Copilot/` � provider unit tests
Unit tests for `ProviderDefinition` JSON loading and `ProviderRegistry`
lookup semantics. No CLI, no workflow runner. **Never construct a real
`GhCopilotCliProvider` and call `ExecuteAsync` / `EnsureReadyAsync` /
`GetAvailableModelsAsync` on it** � those would shell out to `gh`.

### `Commands/` � command-handler integration tests
Tests that construct a single command handler (`LoopCommandHandler`,
`ResumeCommandHandler`, `RespondCommandHandler`, `ProviderCommandHandler`,
`SetupCommandHandler`, `ShellCommandHandler`) directly with a
`MockLlmProvider` and assert on session state, project settings, and
console output. These bypass `CommandLineParser` (argv) but exercise the
full handler logic including path resolution, archiving, blocked-session
handling, and `--step` semantics.

`SetupCommandHandlerTests` and `CommandFailureTests` also drive
`Program.RunAsync` directly for a few cases, but the focus of this layer
is the handler API surface.

### `EndToEnd/` � stepwise CLI workflow tests
End-to-end tests that drive the **real CLI argv pipeline**
(`Program.RunAsync(string[] args, �)`) through multi-command, multi-`--step`
workflows against a temp workspace and a scripted `MockLlmProvider`.

These tests prove that a user can:
  * `setup` a workspace,
  * start a session with `loop <goal> --step` (or `ask` / `act`),
  * step through a workflow one iteration at a time,
  * be blocked by `ask_user`, `respond` to it, and `resume --step`,
  * complete a session with `stop` and have it archived on the next start,
  * see a non-zero exit code and an `error` session when the provider fails.

Use the `CliHarness` in `TestInfrastructure/`. It builds a real
`ProviderRegistry` containing only the supplied `MockLlmProvider` and passes
it into `Program.RunAsync` via the `providerRegistry` parameter, so the CLI
never shells out to `gh copilot`. It also pins `Environment.CurrentDirectory`
and captures stdout, restoring both on dispose.

## Shared infrastructure (`TestInfrastructure/`)

| Type | Purpose |
| --- | --- |
| `TempWorkspace` | Disposable temp directory for workspace roots, install dirs, runtime roots. |
| `MockLlmProvider` | `ILlmProvider` test double with a scripted invocation queue and prompt/model/source assertions. |
| `CliHarness` | End-to-end harness that builds a real `ProviderRegistry` containing only a `MockLlmProvider` and passes it into `Program.RunAsync` via its optional `providerRegistry` parameter, captures stdout, and pins `Environment.CurrentDirectory`. |
| `ConsoleCollectionDefinition` | xUnit `[Collection("Console")]` definition that disables parallelism for tests that mutate process-global state (`Console.Out`, `Environment.CurrentDirectory`). |
| `ForbiddenProductionTypeReferenceTests` | Static-analysis safeguard that scans the test assembly's metadata and IL to ensure no test references `GhProcess` and no test calls `ProviderRegistry.Create(...)`. |

Any test that uses `CliHarness`, redirects `Console.Out`, or changes
`Environment.CurrentDirectory` **must** be in `[Collection("Console")]`.
