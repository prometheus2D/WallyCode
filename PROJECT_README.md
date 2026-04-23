# WallyCode Project Notes

This file is for project-internal terminology and architecture notes. It is intentionally separate from the public/operator-facing `README.md`.

## Working terminology

Current best-fit terminology for this codebase:

- **Definitions** are named routed workflows and practical entry points into WallyCode behavior.
  - In CLI usage, a definition is selected with `wallycode loop --definition <name>`.
  - If omitted, the default definition is currently `requirements`.
  - Examples in this repo: `ask`, `act`, `requirements`, `tasks`, `full-pipeline`.

- **Logical units** are the executable routing steps inside a definition.
  - A logical unit contains instructions, allowed keywords, and optional transitions.
  - A unit is the thing the runner activates on each iteration.
  - Shared logical units can be reused across multiple definitions.

- **Keywords** are the routing decisions emitted by the provider.
  - Shared control keywords should stay small and predictable: `[CONTINUE]`, `[ASK_USER]`, `[DONE]`, and `[ERROR]`.
  - Routing keywords beyond those control outcomes should be definition-specific or unit-specific.

## Is "definitions are basically call commands / starting points" correct?

Mostly yes, with one important refinement:

- A **definition** is not itself a CLI command.
- A definition is a **named workflow selected by the `loop` command**.
- So it is accurate to think of definitions as **discrete starting points / workflow entry points** for WallyCode.

Examples:

- `wallycode loop "..." --definition ask`
- `wallycode loop "..." --definition act`
- `wallycode loop "..." --definition requirements`

## Is "logical units are the programming of Wally" correct?

Yes, that is a reasonable internal shorthand.

More precisely:

- Definitions describe **which units participate and how they are wired together**.
- Logical units contain the **step-level behavior**: instructions, allowed routing outputs, and transitions.
- Shared units plus definition-specific overrides are the closest thing this codebase has to WallyCode's workflow programming model.

## How the current code reflects this

### Definitions

Definitions are represented by `RoutingDefinition` and JSON files under:

- `WallyCode.Console/Routing/Definitions/*.json`

A definition contains:

- `name`
- `startUnitName`
- `units` and/or `unitRefs`

This strongly supports the idea that definitions are named workflow entry points.

### Logical units

Logical units are represented by `LogicalUnit` and shared unit JSON files under:

- `WallyCode.Console/Routing/Units/*.json`

A logical unit contains:

- `name`
- `instructions`
- `allowedKeywords`
- `keywordOptions`
- `transitions`
- optional execution metadata such as `executionKind` and `scriptPath`

This strongly supports the idea that logical units are the step-by-step programmable behavior of the system.

### Runtime behavior

`RoutedRunner` executes the currently active logical unit for the active session.

At runtime it:

1. loads the active session
2. resolves the active unit from the selected definition
3. builds a prompt from the goal, unit instructions, keyword options, and pending user responses
4. asks the provider for a `selectedKeyword`
5. applies either:
   - an explicit transition to another unit, or
   - a built-in control outcome like continue / ask user / done / error

This means the runtime model is:

- **definition = workflow shape / entry point**
- **logical unit = active executable step**
- **keyword = routing decision**

## Keyword model guidance

Preferred model for this project:

- `[CONTINUE]` means remain in the current logical unit.
- `[ASK_USER]` means block and wait for `respond` input.
- `[DONE]` means the workflow is complete.
- `[ERROR]` means the workflow cannot continue because of an unrecoverable problem.
- Any keyword that routes between units should be named for the workflow meaning, such as `[REQUIREMENTS_READY]` or `[TASKS_READY]`, rather than a generic shared routing keyword.

When `[ERROR]` is selected, the provider should put the user-visible reason in the `summary` field.

This keeps the shared keyword surface small while allowing definitions to express their own routing semantics.

## Important nuance

The public `README.md` currently says:

- "Definitions are named routed workflows."

That is correct, but it is less explicit than this internal note about definitions being practical workflow entry points and logical units being the step-level programming model.

## Suggested team shorthand

If the team wants a concise shared phrasing, this is close to the code:

- **Definitions are WallyCode workflow entry points.**
- **Logical units are the workflow steps that implement the behavior.**
- **Keywords are the routing outputs that move or stop the workflow.**

## Current alignment assessment

The codebase mostly reflects this mental model already.

What matches well:

- definitions are named selectable workflows
- definitions choose a start unit
- logical units hold the actual step instructions and routing behavior
- shared units can be reused across definitions
- definition-specific routing keywords already exist in shipped workflows

What should stay constrained:

- definitions are not separate CLI commands; they are inputs to the `loop` command
- generic shared routing keywords should be avoided when a workflow-specific keyword is clearer

## Future documentation guidance

If future developers need to preserve this model, prefer these terms consistently:

- **workflow definition** instead of just "definition" when clarity matters
- **logical unit** for a routed step
- **keyword** for routing output
- **session** for runtime state

That wording matches the current implementation closely.
