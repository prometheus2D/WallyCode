using WallyCode.ConsoleApp.Copilot;

namespace WallyCode.ConsoleApp.Tests.Infrastructure;

// The only LLM provider test double. Tests should inject this through ProviderRegistry.
internal sealed class TestLlmProvider : ILlmProvider
{
    private readonly Queue<Func<CopilotRequest, string>> _responses = new();
    private readonly List<CopilotRequest> _requests = [];

    public string Name { get; init; } = "test-provider";

    public string Description { get; init; } = "Test provider";

    public string DefaultModel { get; init; } = "test-model";

    public IReadOnlyList<string> SupportedModels { get; init; } = ["test-model"];

    public IReadOnlyList<CopilotRequest> Requests => _requests;

    public TestLlmProvider RegisterResponse(string response)
    {
        _responses.Enqueue(_ => response);
        return this;
    }

    public TestLlmProvider RegisterResponse(Func<CopilotRequest, string> responseFactory)
    {
        _responses.Enqueue(responseFactory);
        return this;
    }

    public Task<string?> GetReadinessErrorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(null);
    }

    public Task<string> ExecuteAsync(CopilotRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No test response was registered for TestLlmProvider.");
        }

        return Task.FromResult(_responses.Dequeue()(request));
    }
}
