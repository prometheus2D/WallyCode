# book-story

Use this tutorial when you want WallyCode to help build a story as normal files in a repo.

This works well for:

- a novella outline
- chapter drafts
- character notes
- revision passes
- style guides and continuity notes

## Best fit

Use `act` style requests when you want WallyCode to create or revise files directly.

In the current CLI that means:

```text
loop --definition act "<your request>" --source C:\path\to\your\book-repo
```

## Suggested workspace shape

Start with a plain folder structure like this:

```text
book/
  outline.md
  characters.md
  style-guide.md
  chapters/
    01-opening.md
    02-midpoint.md
  notes/
```

WallyCode works best when the files are simple markdown and the task is concrete.

## Exact first pass

1. Create or choose a repo for the story.
2. Point WallyCode at that repo with `--source`.
3. Ask WallyCode to scaffold the story workspace.

Example:

```text
loop --definition act "Create a book workspace for a 6 chapter cozy fantasy novella. Add outline.md, characters.md, style-guide.md, and chapter stub files under chapters/. Keep the prose voice warm, grounded, and lightly funny." --source C:\src\moon-market-book
```

## Build the story in small passes

After the scaffold exists, keep each request narrow.

Examples:

```text
loop --definition act "Expand outline.md into six short chapter summaries with clear conflict progression." --source C:\src\moon-market-book

loop --definition act "Write the first full draft of chapters/01-opening.md based on outline.md and characters.md. Keep it under 1200 words." --source C:\src\moon-market-book

loop --definition act "Revise chapters/01-opening.md to make the lead character more decisive and cut repetitive phrasing." --source C:\src\moon-market-book
```

## Use ask to review without editing

When you want critique instead of file changes, switch to `ask` style:

```text
loop --definition ask "Review the current outline and list the three weakest story beats. Do not suggest code changes, just explain the story problems." --source C:\src\moon-market-book
```

Good review prompts:

- "Where does the pacing sag?"
- "Which character motivation feels least believable?"
- "What continuity risks should I watch as I draft chapters 3 to 6?"

## A practical writing rhythm

Use this loop for each chapter:

1. Ask for a short critique of the outline or previous chapter.
2. Run an `act` request to draft or revise one file.
3. Read the result yourself.
4. Run another `act` request only for the next concrete improvement.

That rhythm keeps the output usable and avoids asking for too much at once.

## When to switch to the full loop

If the task becomes multi-step, use the default routed loop instead of a single `act` pass.

Example:

```text
loop "Plan the next three chapters, revise the outline, and then draft the next chapter." --source C:\src\moon-market-book
```

If WallyCode blocks and asks for guidance, answer with:

```text
respond "Prefer emotional clarity over extra worldbuilding. Keep chapter length short."
```

## Good constraints to include

These usually improve results:

- target word count
- narrative voice
- point of view
- forbidden cliches
- required story beats
- which files are allowed to change

## End state

You should end up with a repo that behaves like a writing workspace, not a chat log.

That is the main advantage of using WallyCode for story work: the draft, outline, and notes stay as normal files you can inspect and revise over time.