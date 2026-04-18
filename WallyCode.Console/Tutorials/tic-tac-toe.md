# tic-tac-toe

Use this tutorial when you want WallyCode to build a small feature from scratch in a repo.

Tic-tac-toe is a good example because it is small enough to finish quickly but still has real steps:

- choose structure
- build UI
- add game state
- handle win logic
- polish and test

## Best fit

Use the default `loop` workflow for this tutorial.

That gives WallyCode room to break the task into stages and ask for guidance if it gets blocked.

## Exact first pass

Start with a clear goal and a few constraints.

```text
loop "Build a simple browser-based tic-tac-toe game in this repo. Use plain HTML, CSS, and JavaScript unless the repo already uses a framework. Add win detection, reset, and a clean layout." --source C:\src\tic-tac-toe-demo
```

## Continue the run

If the session is active, continue it with:

```text
loop --source C:\src\tic-tac-toe-demo
```

If you want several steps at once:

```text
loop --steps 3 --source C:\src\tic-tac-toe-demo
```

## If WallyCode asks you something

Reply with `respond` and then continue the loop.

Examples:

```text
respond "Keep it as a single page app with no build step."
loop --source C:\src\tic-tac-toe-demo
```

```text
respond "Use the existing React app instead of adding plain HTML files."
loop --source C:\src\tic-tac-toe-demo
```

## Good constraints to give early

These reduce wasted iterations:

- whether to use plain web files or the existing framework
- whether the game should support score keeping
- whether tests are required
- whether accessibility and keyboard support matter for this pass
- which folders are allowed to change

## A stronger starter prompt

If you want a more directed build, use something like this:

```text
loop "Build a browser-based tic-tac-toe game in this repo. Keep the implementation small. Add a 3x3 board, player turn indicator, win and draw detection, reset button, and basic responsive styling. Add tests if the repo already has a test setup." --source C:\src\tic-tac-toe-demo
```

## Review the result without editing

After the feature is built, switch to `ask` style for a review pass.

```text
loop --definition ask "Review the tic-tac-toe implementation for likely bugs, missing edge cases, and test gaps." --source C:\src\tic-tac-toe-demo
```

## Common rhythm

This pattern works well:

1. Start with a concrete build goal.
2. Let WallyCode run one to three steps.
3. Answer any blocking question.
4. Run a review pass with `ask`.
5. Run one last `act` or `loop` request for cleanup.

## End state

By the end of the tutorial, you should have a working small app and a repeatable example of how to use the routed loop for normal feature work.