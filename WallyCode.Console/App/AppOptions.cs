namespace WallyCode.ConsoleApp.App;

internal sealed class AppOptions
{
    public required string Goal { get; init; }

    public required string ProviderName { get; init; }

    public string? Model { get; init; }

    public string? SourcePath { get; init; }

    public int MaxIterations { get; init; } = 1;
}