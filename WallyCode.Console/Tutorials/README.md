# WallyCode Tutorials

These markdown files are the tutorial source material for WallyCode.

The intended command-line flow is:

```text
wallycode setup --directory C:\src\my-repo
cd C:\src\my-repo
tutorial --list
tutorial repo-review
tutorial book-story
tutorial tic-tac-toe
```

Each tutorial is kept simple on purpose:

- what the workflow is for
- which command shape to use
- exact example commands
- what to do next if the workflow blocks or needs refinement

## Available tutorials

- [book-story](book-story.md) - build a story workspace with markdown files and revise it using `act` style prompts.
- [repo-review](repo-review.md) - review a repository without file changes using `ask` style prompts.
- [tic-tac-toe](tic-tac-toe.md) - build a small browser game with the routed loop and respond as needed.

## Command language note

The README talks about `ask` and `act` as workflow styles.

In the current CLI, those styles are also available as direct commands:

```text
ask "Explain this repository." --source C:\src\my-repo
act "Implement the requested file changes." --source C:\src\my-repo
```

If you prefer to keep running from somewhere else, the tutorial command examples that use `--source` still work.