namespace WallyCode.ConsoleApp.Loop;

internal sealed class LoopIterationResponse
{
    public string Status { get; set; } = "continue";

    public string Summary { get; set; } = string.Empty;

    public string WorkLog { get; set; } = string.Empty;

    public List<string> Questions { get; set; } = [];

    public List<string> Decisions { get; set; } = [];

    public List<string> Assumptions { get; set; } = [];

    public List<string> Blockers { get; set; } = [];

    public string DoneReason { get; set; } = string.Empty;

    public bool IsDone => string.Equals(Status, "done", StringComparison.OrdinalIgnoreCase);

    public void Validate()
    {
        if (!string.Equals(Status, "continue", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Status, "done", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Status must be 'continue' or 'done'.");
        }

        if (string.IsNullOrWhiteSpace(Summary))
        {
            throw new InvalidOperationException("Summary is required.");
        }

        if (IsDone && string.IsNullOrWhiteSpace(DoneReason))
        {
            throw new InvalidOperationException("DoneReason is required when status is 'done'.");
        }
    }
}
