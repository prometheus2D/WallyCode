using System.Text;
using System.Text.Json;
using WallyCode.ConsoleApp.App;
using WallyCode.ConsoleApp.Loop;

namespace WallyCode.ConsoleApp.Runtime;

internal sealed class MemoryWorkspace
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public required string RootPath { get; init; }
    public required string MemoryDirectoryPath { get; init; }
    public required string LogsDirectoryPath { get; init; }
    public required string PromptsDirectoryPath { get; init; }
    public required string RawDirectoryPath { get; init; }
    public required string GoalFilePath { get; init; }
    public required string CurrentTasksFilePath { get; init; }
    public required string PerspectivesFilePath { get; init; }
    public required string NextStepsFilePath { get; init; }
    public required string CurrentStateFilePath { get; init; }
    public required string UserResponsesFilePath { get; init; }
    public required string UserResponsesJsonFilePath { get; init; }
    public required string LoopStateFilePath { get; init; }
    public required string SessionLogFilePath { get; init; }
    public required string SessionStateFilePath { get; init; }

    public static MemoryWorkspace Open(string sourcePath, string? memoryRoot)
    {
        var rootPath = ResolveRootPath(sourcePath, memoryRoot);
        return OpenResolved(rootPath);
    }

    public static MemoryWorkspace Reset(string sourcePath, string? memoryRoot)
    {
        var rootPath = ResolveRootPath(sourcePath, memoryRoot);

        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        return OpenResolved(rootPath);
    }

    private static MemoryWorkspace OpenResolved(string rootPath)
    {
        var memoryPath = Path.Combine(rootPath, "memory");
        var logsPath = Path.Combine(rootPath, "logs");
        var promptsPath = Path.Combine(rootPath, "prompts");
        var rawPath = Path.Combine(rootPath, "raw");

        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(memoryPath);
        Directory.CreateDirectory(logsPath);
        Directory.CreateDirectory(promptsPath);
        Directory.CreateDirectory(rawPath);

        return new MemoryWorkspace
        {
            RootPath = rootPath,
            MemoryDirectoryPath = memoryPath,
            LogsDirectoryPath = logsPath,
            PromptsDirectoryPath = promptsPath,
            RawDirectoryPath = rawPath,
            GoalFilePath = Path.Combine(memoryPath, "goal.md"),
            CurrentTasksFilePath = Path.Combine(memoryPath, "current-tasks.md"),
            PerspectivesFilePath = Path.Combine(memoryPath, "perspectives.md"),
            NextStepsFilePath = Path.Combine(memoryPath, "next-steps.md"),
            CurrentStateFilePath = Path.Combine(memoryPath, "current-state.md"),
            UserResponsesFilePath = Path.Combine(memoryPath, "user-responses.md"),
            UserResponsesJsonFilePath = Path.Combine(rootPath, "responses.json"),
            LoopStateFilePath = Path.Combine(rootPath, "state.json"),
            SessionLogFilePath = Path.Combine(logsPath, "session.log"),
            SessionStateFilePath = Path.Combine(rootPath, "session.json")
        };
    }

    public LoopSessionState StartNewSession(AppOptions options, LoopTemplate template)
    {
        ResetDirectory(MemoryDirectoryPath);
        ResetDirectory(LogsDirectoryPath);
        ResetDirectory(PromptsDirectoryPath);
        ResetDirectory(RawDirectoryPath);

        WriteDocument(GoalFilePath, $$"""
# Goal

{{options.Goal.Trim()}}
""");

        WriteDocument(CurrentTasksFilePath, string.IsNullOrWhiteSpace(template.InitialCurrentTasks)
            ? "# Current Tasks\n\n1. Run one bounded iteration."
            : template.InitialCurrentTasks);

        WriteDocument(PerspectivesFilePath, string.IsNullOrWhiteSpace(template.InitialPerspectives)
            ? "# Perspectives"
            : template.InitialPerspectives);

        WriteDocument(NextStepsFilePath, string.IsNullOrWhiteSpace(template.InitialNextSteps)
            ? "# Next Steps\n\n1. Run the next iteration."
            : template.InitialNextSteps);

        WriteDocument(CurrentStateFilePath, $$"""
{{(string.IsNullOrWhiteSpace(template.InitialCurrentState) ? "# Current State" : template.InitialCurrentState)}}

- Goal: {{options.Goal.Trim()}}
- Provider: {{options.ProviderName}}
- Source path: {{options.SourcePath ?? "Not provided"}}
- Memory root: {{RootPath}}
- Requested steps in this run: {{options.MaxIterations}}
- Model: {{options.Model ?? "Default"}}
- Loop template: {{template.TemplateId}}
- Session started: {{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}}
""");

        WriteDocument(UserResponsesFilePath, "# User Responses\n");
        SaveUserResponseStore(new UserResponseStore());
        SaveLoopState(new LoopState());
        File.WriteAllText(SessionLogFilePath, string.Empty, Utf8NoBom);

        var session = new LoopSessionState
        {
            Goal = options.Goal.Trim(),
            ProviderName = options.ProviderName,
            Model = options.Model,
            SourcePath = options.SourcePath ?? Environment.CurrentDirectory,
            NextIteration = 1,
            IsDone = false,
            DoneReason = string.Empty,
            StartedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            LoopTemplateId = template.TemplateId
        };

        SaveSession(session);
        return session;
    }

    public LoopSessionState? TryLoadSession()
    {
        if (!File.Exists(SessionStateFilePath))
        {
            return null;
        }

        var session = JsonSerializer.Deserialize<LoopSessionState>(File.ReadAllText(SessionStateFilePath), SerializerOptions)
            ?? throw new InvalidOperationException($"The loop session file is invalid: {SessionStateFilePath}");

        session.Validate();
        return session;
    }

    public void SaveSession(LoopSessionState session)
    {
        session.Validate();
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var json = JsonSerializer.Serialize(session, SerializerOptions);
        File.WriteAllText(SessionStateFilePath, json + Environment.NewLine, Utf8NoBom);
    }

    public LoopState LoadLoopState()
    {
        if (!File.Exists(LoopStateFilePath))
        {
            return new LoopState();
        }

        return JsonSerializer.Deserialize<LoopState>(File.ReadAllText(LoopStateFilePath), SerializerOptions)
            ?? new LoopState();
    }

    public void SaveLoopState(LoopState state)
    {
        var json = JsonSerializer.Serialize(state, SerializerOptions);
        File.WriteAllText(LoopStateFilePath, json + Environment.NewLine, Utf8NoBom);
    }

    public MemorySnapshot ReadSnapshot(LoopState state)
    {
        var store = LoadUserResponseStore();
        var pendingResponses = store.Responses
            .Where(response => response.Id > state.LastProcessedUserResponseId)
            .OrderBy(response => response.Id)
            .ToList();

        return new MemorySnapshot
        {
            Goal = ReadDocument(GoalFilePath),
            CurrentTasks = ReadDocument(CurrentTasksFilePath),
            Perspectives = ReadDocument(PerspectivesFilePath),
            NextSteps = ReadDocument(NextStepsFilePath),
            CurrentState = ReadDocument(CurrentStateFilePath),
            UserResponses = ReadDocument(UserResponsesFilePath),
            PendingUserResponses = pendingResponses
        };
    }

    public void SavePrompt(int iteration, string prompt)
    {
        File.WriteAllText(GetPromptPath(iteration), Normalize(prompt), Utf8NoBom);
    }

    public void SaveRawOutput(int iteration, string rawOutput)
    {
        File.WriteAllText(GetRawOutputPath(iteration), Normalize(rawOutput), Utf8NoBom);
    }

    public void ApplyIteration(int iteration, LoopIterationResponse response, string currentTasks, string nextSteps, string currentState)
    {
        WriteDocument(CurrentTasksFilePath, currentTasks);
        WriteDocument(NextStepsFilePath, nextSteps);
        WriteDocument(CurrentStateFilePath, currentState);

        var iterationLogPath = Path.Combine(LogsDirectoryPath, $"iteration-{iteration:000}.md");
        var doneReason = string.IsNullOrWhiteSpace(response.DoneReason) ? "N/A" : response.DoneReason;

        File.WriteAllText(iterationLogPath, $$"""
# Iteration {{iteration}}

- Timestamp: {{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}}
- Status: {{response.Status}}
- Summary: {{response.Summary}}
- Done reason: {{doneReason}}

## Questions

{{RenderList(response.Questions)}}

## Decisions

{{RenderList(response.Decisions)}}

## Assumptions

{{RenderList(response.Assumptions)}}

## Blockers

{{RenderList(response.Blockers)}}

## Work Log

{{response.WorkLog}}
""", Utf8NoBom);
    }

    public void AppendUserResponse(string response)
    {
        var content = response.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("A non-empty user response is required.");
        }

        var store = LoadUserResponseStore();
        var entry = new UserResponseEntry
        {
            Id = store.Responses.Count == 0 ? 1 : store.Responses.Max(item => item.Id) + 1,
            TimestampUtc = DateTimeOffset.UtcNow,
            Text = content
        };

        store.Responses.Add(entry);
        SaveUserResponseStore(store);

        if (!File.Exists(UserResponsesFilePath))
        {
            WriteDocument(UserResponsesFilePath, "# User Responses\n");
        }

        File.AppendAllText(UserResponsesFilePath, $$"""

## {{entry.TimestampUtc:yyyy-MM-dd HH:mm:ss zzz}} | Response {{entry.Id}}

{{entry.Text}}
""", Utf8NoBom);
    }

    public string GetPromptPath(int iteration)
    {
        return Path.Combine(PromptsDirectoryPath, $"iteration-{iteration:000}.txt");
    }

    public string GetRawOutputPath(int iteration)
    {
        return Path.Combine(RawDirectoryPath, $"iteration-{iteration:000}.txt");
    }

    private UserResponseStore LoadUserResponseStore()
    {
        if (!File.Exists(UserResponsesJsonFilePath))
        {
            return new UserResponseStore();
        }

        return JsonSerializer.Deserialize<UserResponseStore>(File.ReadAllText(UserResponsesJsonFilePath), SerializerOptions)
            ?? new UserResponseStore();
    }

    private void SaveUserResponseStore(UserResponseStore store)
    {
        var json = JsonSerializer.Serialize(store, SerializerOptions);
        File.WriteAllText(UserResponsesJsonFilePath, json + Environment.NewLine, Utf8NoBom);
    }

    private static string ResolveRootPath(string sourcePath, string? memoryRoot)
    {
        if (!string.IsNullOrWhiteSpace(memoryRoot))
        {
            return Path.GetFullPath(memoryRoot);
        }

        var basePath = Path.GetFullPath(sourcePath);
        return Path.Combine(basePath, ".wallycode");
    }

    private static string ReadDocument(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static void WriteDocument(string path, string content)
    {
        File.WriteAllText(path, Normalize(content), Utf8NoBom);
    }

    private static void ResetDirectory(string path)
    {
        Directory.CreateDirectory(path);

        foreach (var filePath in Directory.EnumerateFiles(path))
        {
            File.Delete(filePath);
        }
    }

    private static string Normalize(string content)
    {
        return content.Trim() + Environment.NewLine;
    }

    private static string RenderList(IEnumerable<string> items)
    {
        var values = items.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        return values.Count == 0
            ? "- None"
            : string.Join(Environment.NewLine, values.Select(value => $"- {value.Trim()}"));
    }
}