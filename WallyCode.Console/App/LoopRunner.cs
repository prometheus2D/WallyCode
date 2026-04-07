using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Loop;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.App;

internal sealed class LoopRunner
{
    private readonly ILlmProvider _provider;
    private readonly AppLogger _logger;

    public LoopRunner(ILlmProvider provider, AppLogger logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task RunAsync(AppOptions options, MemoryWorkspace workspace, LoopSessionState session, CancellationToken cancellationToken)
    {
        for (var step = 1; step <= options.MaxIterations; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iteration = session.NextIteration;

            _logger.Section($"Iteration {iteration} ({step}/{options.MaxIterations} in this run)");
            _logger.Info("Reading current memory state.");

            var snapshot = workspace.ReadSnapshot();
            var prompt = LoopPromptBuilder.Build(options, workspace, snapshot, iteration, step);
            workspace.SavePrompt(iteration, prompt);

            _logger.Info($"Calling provider {options.ProviderName}.");

            var rawOutput = await _provider.ExecuteAsync(
                new CopilotRequest
                {
                    Prompt = prompt,
                    Model = options.Model,
                    SourcePath = options.SourcePath
                },
                cancellationToken);

            workspace.SaveRawOutput(iteration, rawOutput);
            _logger.Info("Provider output saved. Parsing structured response.");

            LoopIterationResponse response;

            try
            {
                response = LoopResponseParser.Parse(rawOutput);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Iteration {iteration} returned an invalid structured response. Inspect {workspace.GetRawOutputPath(iteration)} for details.",
                    exception);
            }

            workspace.ApplyIteration(iteration, response);
            session.NextIteration = iteration + 1;
            session.IsDone = response.IsDone;
            session.DoneReason = response.DoneReason;
            workspace.SaveSession(session);

            _logger.Info($"Summary: {response.Summary}");

            if (response.IsDone)
            {
                if (!string.IsNullOrWhiteSpace(response.DoneReason))
                {
                    _logger.Info($"Done reason: {response.DoneReason}");
                }

                return;
            }

            _logger.Info("Memory updated for the next loop.");
        }

        _logger.Warning("Requested steps complete. Run loop to continue the session.");
    }
}