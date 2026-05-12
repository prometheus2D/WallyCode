# AGENTS

## Change Policy

- Do not introduce or preserve legacy compatibility logic by default.
- When a user asks to make changes, treat that as authorization to remove obsolete aliases, deprecated flags, and backward-compatibility branches.
- Only include legacy support when the user explicitly requests it.

## Validation And Tests Policy

- Do not run tests, test suites, check commands, lint commands, build verification, browser automation, or other validation commands unless the user explicitly asks for them in the current request.
- When the user asks for changes, treat existing tests as legacy behavior if they conflict with the requested change. Update failing tests to match the new requested behavior rather than changing the request or implementation back to satisfy old expectations, unless the user explicitly says otherwise.
- When changing code, explain what should be tested or checked manually instead of running validation commands proactively.
- If the user explicitly asks to run tests or validation, run only the requested command or the narrowest relevant command.
