namespace WallyCode.ConsoleApp.Copilot;

internal interface ILlmProvider
{
    string Name { get; }

    string Description { get; }

    string DefaultModel { get; }

    IReadOnlyList<string> SupportedModels { get; }

    Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(SupportedModels);

    Task<string?> GetReadinessErrorAsync(CancellationToken cancellationToken);

    async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        var readinessError = await GetReadinessErrorAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(readinessError))
        {
            throw new InvalidOperationException(readinessError);
        }
    }

    Task<string> ExecuteAsync(CopilotRequest request, CancellationToken cancellationToken);
}
