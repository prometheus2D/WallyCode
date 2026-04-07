namespace WallyCode.ConsoleApp.Runtime;

internal sealed class LoopSessionState
{
    public string Goal { get; set; } = string.Empty;

    public string ProviderName { get; set; } = string.Empty;

    public string? Model { get; set; }

    public string SourcePath { get; set; } = string.Empty;

    public int NextIteration { get; set; } = 1;

    public bool IsDone { get; set; }

    public string DoneReason { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Goal))
        {
            throw new InvalidOperationException("The loop session goal is required.");
        }

        if (string.IsNullOrWhiteSpace(ProviderName))
        {
            throw new InvalidOperationException("The loop session provider is required.");
        }

        if (string.IsNullOrWhiteSpace(SourcePath))
        {
            throw new InvalidOperationException("The loop session source path is required.");
        }

        if (NextIteration <= 0)
        {
            throw new InvalidOperationException("The next iteration must be greater than zero.");
        }
    }
}