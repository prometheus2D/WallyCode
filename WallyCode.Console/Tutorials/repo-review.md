# repo-review

Use this tutorial when you want WallyCode to review a repository without changing files.

This is the right fit for:

- architecture summaries
- codebase tours
- risk reviews
- test gap reviews
- onboarding questions

## Best fit

Use `ask` style requests when you want analysis only.

The underlying routed form is:

```text
loop --definition ask "<your question>" --source C:\path\to\repo
```

The shortcut form is:

```text
ask "<your question>" --source C:\path\to\repo
```

## Exact first pass

Start with a small summary request.

```text
loop --definition ask "Summarize this repository in one short paragraph. Focus on what it does, how it is structured, and where the entry point is." --source C:\src\my-repo
```

That gives you a quick map before you ask deeper questions.

## Follow-up questions that work well

Ask one concrete question at a time.

Examples:

```text
loop --definition ask "List the main runtime flows in this repo and name the key files involved in each one." --source C:\src\my-repo

loop --definition ask "Review this repository for the three biggest correctness risks. Focus on likely behavior bugs, not style." --source C:\src\my-repo

loop --definition ask "What parts of this repository look least covered by tests? Explain why." --source C:\src\my-repo

loop --definition ask "If I needed to add a setup command, where would that code likely belong and what files would need to change?" --source C:\src\my-repo
```

## Good review passes

These review passes are usually useful in order:

1. repository summary
2. key command surface
3. config and file layout
4. risk review
5. test gap review
6. implementation plan for the next change

## Suggested sequence

Example sequence:

```text
loop --definition ask "Summarize this repository in one short paragraph." --source C:\src\my-repo

loop --definition ask "Explain the command surface and which commands are user-facing." --source C:\src\my-repo

loop --definition ask "Review the setup story for this repo. What already works, and what still creates friction for a new developer?" --source C:\src\my-repo
```

## When to stop using ask

Switch to `act` or the default `loop` when you are done reviewing and want changes made.

Examples:

```text
loop --definition act "Implement the setup command described in the plan." --source C:\src\my-repo

loop "Design and implement an improved tutorial system for this repo." --source C:\src\my-repo
```

## Common mistake to avoid

Do not ask for a huge review and a code change in the same `ask` request.

Keep `ask` focused on understanding. Move to `act` only when you want the repo changed.