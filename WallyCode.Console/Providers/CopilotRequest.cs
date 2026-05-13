namespace WallyCode.ConsoleApp.Copilot;

internal sealed class CopilotRequest
{
    public required string Prompt { get; init; }

    public string? Model { get; init; }

    public string? SourcePath { get; init; }
}