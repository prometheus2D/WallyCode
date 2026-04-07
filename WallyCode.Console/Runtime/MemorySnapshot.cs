namespace WallyCode.ConsoleApp.Runtime;

internal sealed class MemorySnapshot
{
    public required string Goal { get; init; }

    public required string CurrentTasks { get; init; }

    public required string Perspectives { get; init; }

    public required string NextSteps { get; init; }

    public required string CurrentState { get; init; }

    public required string UserResponses { get; init; }

    public required IReadOnlyList<UserResponseEntry> PendingUserResponses { get; init; }
}