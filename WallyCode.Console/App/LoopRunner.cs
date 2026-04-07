using System.Text.RegularExpressions;
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
        var template = LoopTemplateRegistry.Load(options.LoopTemplateId);
        var state = workspace.LoadLoopState();

        for (var step = 1; step <= options.MaxIterations; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iteration = session.NextIteration;

            _logger.Section($"Iteration {iteration} ({step}/{options.MaxIterations} in this run)");
            _logger.Info("Reading current memory state.");

            var snapshot = workspace.ReadSnapshot(state);
            var prompt = LoopPromptBuilder.Build(options, workspace, snapshot, iteration, step, template);
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

            ApplyStopKeywordIfMatched(template, snapshot, response, state);
            UpdateLoopState(state, response, snapshot.PendingUserResponses);
            workspace.SaveLoopState(state);
            workspace.ApplyIteration(
                iteration,
                response,
                LoopMemoryRenderer.RenderCurrentTasks(state),
                LoopMemoryRenderer.RenderNextSteps(state),
                LoopMemoryRenderer.RenderCurrentState(session, state, response));

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

    private static void ApplyStopKeywordIfMatched(LoopTemplate template, MemorySnapshot snapshot, LoopIterationResponse response, LoopState state)
    {
        if (string.IsNullOrWhiteSpace(template.StopKeyword)
            || response.IsDone
            || snapshot.PendingUserResponses.Count == 0)
        {
            return;
        }

        if (!snapshot.PendingUserResponses.Any(item => item.Text.Contains(template.StopKeyword, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        state.StopKeywordMatched = true;
        response.Status = "done";
        response.DoneReason = $"The configured stop keyword '{template.StopKeyword}' was found in pending user responses.";
    }

    private static void UpdateLoopState(LoopState state, LoopIterationResponse response, IReadOnlyList<UserResponseEntry> pendingResponses)
    {
        state.Phase = response.IsDone
            ? "done"
            : response.Questions.Count > 0
                ? "waiting-for-user"
                : "active";
        state.OpenQuestions = response.Questions
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();
        state.Decisions = response.Decisions
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();

        if (pendingResponses.Count > 0)
        {
            var lastResponse = pendingResponses[^1];
            state.LastProcessedUserResponseId = lastResponse.Id;
            state.LastProcessedUserResponseAt = lastResponse.TimestampUtc.ToString("O");
        }
    }
}