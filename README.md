# WallyCode

WallyCode is a .NET 8 command-line tool for running a routed LLM workflow against a local repository or folder.

It can:
- initialize a project for WallyCode
- list and configure LLM providers
- start or continue routed sessions
- pause for user input when a session blocks
- log prompts, responses, and transitions for inspection

## Prerequisites

Before using WallyCode, make sure you have:
- .NET 8 SDK or runtime available
- GitHub Copilot CLI installed and authenticated if you want to use the built-in GitHub Copilot providers

This workspace currently includes these provider names:
- `gh-copilot-claude`
- `gh-copilot-gpt5`

## Build

From the repository root:

```powershell
dotnet build
```

If you want to run the app without installing it globally, use:

```powershell
dotnet run --project .\WallyCode.Console -- --help
```

In the examples below, `wallycode` means either:
- a published/installed executable named `wallycode`, or
- `dotnet run --project .\WallyCode.Console --`

Example:

```powershell
dotnet run --project .\WallyCode.Console -- provider
```

## Quick start

### 1. Build the project

```powershell
dotnet build
```

### 2. Initialize your target folder

Run setup in the repository or folder you want WallyCode to work against.

```powershell
wallycode setup --directory C:\path\to\your\repo
```

This creates:
- `wallycode.json`
- `.wallycode\`

If you are already in the target folder and running the app there, you can also use:

```powershell
wallycode setup
```

To recreate setup artifacts:

```powershell
wallycode setup --directory C:\path\to\your\repo --force
```

### 3. Check available providers

```powershell
wallycode provider --source C:\path\to\your\repo
```

This lists configured providers and shows which one is the current default.

### 4. Set the default provider

Example:

```powershell
wallycode provider gh-copilot-claude --set --source C:\path\to\your\repo
```

Or:

```powershell
wallycode provider gh-copilot-gpt5 --set --source C:\path\to\your\repo
```

### 5. List models for a provider

```powershell
wallycode provider gh-copilot-claude --models --source C:\path\to\your\repo
```

### 6. Set the default model

```powershell
wallycode provider gh-copilot-claude --model claude-sonnet-4 --source C:\path\to\your\repo
```

Another example:

```powershell
wallycode provider gh-copilot-gpt5 --model gpt-5 --source C:\path\to\your\repo
```

### 7. Start a session

Start a routed session with a goal:

```powershell
wallycode loop "Summarize this repository in one short paragraph." --source C:\path\to\your\repo
```

### 8. Continue the active session

If a session already exists and is still active:

```powershell
wallycode loop --source C:\path\to\your\repo
```

### 9. Respond when the session asks for input

If the session becomes blocked and asks for user input:

```powershell
wallycode respond "Use CSV output." --source C:\path\to\your\repo
```

Then continue the loop:

```powershell
wallycode loop --source C:\path\to\your\repo
```

## Core commands

## `setup`

Initializes WallyCode in a target directory.

```powershell
wallycode setup [--directory <path>] [--vs-build] [--force]
```

Options:
- `--directory <path>`: target directory for setup
- `--vs-build`: resolve the setup target from a standard Visual Studio build output path
- `--force`: recreate setup artifacts even if they already exist

Examples:

```powershell
wallycode setup --directory C:\src\MyRepo
wallycode setup --directory C:\src\MyRepo --force
```

## `provider`

Lists providers, lists models, refreshes discovered models, or sets the default provider/model.

```powershell
wallycode provider [name] [--set] [--models] [--refresh] [--model <model>] [--source <path>]
```

Common examples:

List providers for a project:

```powershell
wallycode provider --source C:\src\MyRepo
```

Set the default provider:

```powershell
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
```

List models for a provider:

```powershell
wallycode provider gh-copilot-gpt5 --models --source C:\src\MyRepo
```

Refresh discovered models for a provider:

```powershell
wallycode provider gh-copilot-gpt5 --refresh --source C:\src\MyRepo
```

Set the default model for the current or selected provider:

```powershell
wallycode provider gh-copilot-gpt5 --model gpt-5 --source C:\src\MyRepo
```

Notes:
- `--source` points at the repo or folder whose `wallycode.json` should be updated
- if `name` is omitted for `--models`, `--refresh`, or `--model`, WallyCode uses the current project default provider

## `loop`

Runs the routing engine against a routing definition.

```powershell
wallycode loop [goal] [--definition <name>] [--provider <name>] [--model <model>] [--source <path>] [--memory-root <path>] [--steps <n>] [--log] [--verbose]
```

Behavior:
- if there is no active session, provide a `goal` to start one
- if there is an active session, omit `goal` to continue it
- if the session is blocked, use `respond` first
- if the session is completed or failed, WallyCode archives it before starting a new one

Examples:

Start a new session with the default definition:

```powershell
wallycode loop "Summarize this repository in one short paragraph." --source C:\src\MyRepo
```

Start a session with the `ask` definition:

```powershell
wallycode loop "What does this project do?" --definition ask --source C:\src\MyRepo
```

Start a session with the `act` definition:

```powershell
wallycode loop "Refactor the routing code for readability." --definition act --source C:\src\MyRepo
```

Run multiple iterations in one invocation:

```powershell
wallycode loop "Review the repository structure." --steps 3 --source C:\src\MyRepo
```

Override provider and model for a new session:

```powershell
wallycode loop "Summarize the tests." --provider gh-copilot-gpt5 --model gpt-5 --source C:\src\MyRepo
```

Continue the current session:

```powershell
wallycode loop --source C:\src\MyRepo
```

Use a separate session state folder:

```powershell
wallycode loop "Analyze docs." --source C:\src\MyRepo --memory-root C:\temp\wally-session
```

## `respond`

Appends a user response for the active loop session.

```powershell
wallycode respond <response> [--source <path>] [--memory-root <path>] [--log] [--verbose]
```

Example:

```powershell
wallycode respond "Prefer bullet points and keep it short." --source C:\src\MyRepo
```

Then continue:

```powershell
wallycode loop --source C:\src\MyRepo
```

## Logging and stepping through transitions

WallyCode supports invocation logging on these commands:
- `loop`
- `ask`
- `act`
- `respond`
- `shell`

Use:

```powershell
wallycode loop "Summarize this repository." --source C:\src\MyRepo --log
```

For more detailed logging:

```powershell
wallycode loop "Summarize this repository." --source C:\src\MyRepo --log --verbose
```

This is the current user-facing way to inspect:
- prompts sent to the provider
- provider responses
- iteration-by-iteration transitions
- session status changes

## Definitions

The routed workflow supports named definitions.

Examples currently covered by tests include:
- `ask`
- `act`

If you do not specify `--definition`, `loop` defaults to `requirements`.

## Project files created and used by WallyCode

In your target project folder:
- `wallycode.json`: project settings such as default provider and model
- `.wallycode\`: runtime/session state

Session state may include:
- `session.json`
- archived sessions under `.wallycode\archive\`
- logs and transcripts when logging is enabled

## Typical first-use flow

```powershell
wallycode setup --directory C:\src\MyRepo
wallycode provider --source C:\src\MyRepo
wallycode provider gh-copilot-claude --set --source C:\src\MyRepo
wallycode provider gh-copilot-claude --model claude-sonnet-4 --source C:\src\MyRepo
wallycode loop "Summarize this repository in one short paragraph." --source C:\src\MyRepo --log
```

If the session blocks:

```powershell
wallycode respond "Focus on the routing and test structure." --source C:\src\MyRepo --log
wallycode loop --source C:\src\MyRepo --log
```

## Troubleshooting

If a provider is unavailable:
- verify the required external tooling is installed
- verify you are authenticated for that provider
- run `wallycode provider --source <path>` to inspect provider readiness

If `loop` says there is no active session:
- start one with `wallycode loop "<goal>" --source <path>`

If a session is blocked:
- use `wallycode respond "<message>" --source <path>`
- then run `wallycode loop --source <path>` again

