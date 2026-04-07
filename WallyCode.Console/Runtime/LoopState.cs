namespace WallyCode.ConsoleApp.Runtime;

internal sealed class LoopState
{
    public string Phase { get; set; } = "active";

    public List<string> OpenQuestions { get; set; } = [];

    public List<string> Decisions { get; set; } = [];

    public bool StopKeywordMatched { get; set; }

    public string? LastProcessedUserResponseAt { get; set; }

    public int LastProcessedUserResponseId { get; set; }
}
