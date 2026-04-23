using WallyCode.ConsoleApp.Copilot;

namespace WallyCode.Tests.TestInfrastructure;

internal sealed class MockInvocation
{
    public string? RawOutput { get; set; }
    public Exception? Exception { get; set; }
    public string? ExpectedPrompt { get; set; }
    public string? ExpectedModel { get; set; }
    public string? ExpectedSourcePath { get; set; }
    public string? Label { get; set; }
}

internal sealed class MockLlmProvider : ILlmProvider
{
    private readonly List<MockInvocation> _script;
    private readonly List<CopilotRequest> _requests = [];
    private readonly string? _readinessError;
    private int _cursor;

    public MockLlmProvider(IEnumerable<MockInvocation> script, string? readinessError = null)
    {
        _script = [.. script];
        _readinessError = readinessError;
    }

    public string Name { get; init; } = "mock-provider";
    public string Description { get; init; } = "Test double for ILlmProvider.";
    public string DefaultModel { get; init; } = "mock-default-model";
    public IReadOnlyList<string> SupportedModels { get; init; } = ["mock-default-model", "mock-alt-model"];

    public IReadOnlyList<CopilotRequest> Requests => _requests;
    public int ConsumedCount => _cursor;

    public Task<string?> GetReadinessErrorAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_readinessError);

    public Task<string> ExecuteAsync(CopilotRequest request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (_cursor >= _script.Count)
        {
            throw new InvalidOperationException(
                $"MockLlmProvider received an unexpected invocation #{_cursor + 1}. Scripted {_script.Count} invocations.");
        }

        var invocation = _script[_cursor];
        _cursor++;

        AssertExpectation(invocation.ExpectedPrompt, request.Prompt, nameof(request.Prompt), invocation.Label);
        AssertExpectation(invocation.ExpectedModel, request.Model, nameof(request.Model), invocation.Label);
        AssertExpectation(invocation.ExpectedSourcePath, request.SourcePath, nameof(request.SourcePath), invocation.Label);

        if (invocation.Exception is not null)
        {
            throw invocation.Exception;
        }

        return Task.FromResult(invocation.RawOutput ?? string.Empty);
    }

    public void AssertConsumed()
    {
        if (_cursor != _script.Count)
        {
            throw new InvalidOperationException(
                $"MockLlmProvider consumed {_cursor}/{_script.Count} scripted invocations.");
        }
    }

    private static void AssertExpectation(string? expected, string? actual, string field, string? label)
    {
        if (expected is null)
        {
            return;
        }

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            var prefix = string.IsNullOrWhiteSpace(label) ? string.Empty : $"[{label}] ";
            throw new InvalidOperationException(
                $"{prefix}Expected {field} = '{expected}' but got '{actual}'.");
        }
    }
}
