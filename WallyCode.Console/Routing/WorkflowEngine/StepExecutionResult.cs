namespace WallyCode.ConsoleApp.Workflow;

internal sealed class StepExecutionResult
{
    public string? SelectedStep { get; init; }
    public string Summary { get; init; } = string.Empty;
    public Dictionary<string, string?> MemoryUpdates { get; init; } = [];
}

internal sealed class StepExecutionContext
{
    public required WorkflowDefinition Definition { get; init; }
    public required WorkflowStep Step { get; init; }
    public required Sessions.Session Session { get; init; }
    public required string SessionRoot { get; init; }
    public required string GlobalPrompt { get; init; }
}

internal interface IStepExecutor
{
    string ExecutionKind { get; }

    Task<StepExecutionResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken);
}

internal sealed class IterationResult
{
    public required int IterationNumber { get; init; }
    public required string SelectedStep { get; init; }
    public required string Summary { get; init; }
    public required string ActiveStepName { get; init; }
    public required string Status { get; init; }
    public required bool StopsInvocation { get; init; }
}