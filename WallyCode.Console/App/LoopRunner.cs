using System.Text.RegularExpressions;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Loop;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.App;

internal sealed class LoopRunner
{
    private static readonly Regex NumberedStepRegex = new(@"^\s*\d+\.\s+(.*\S)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

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

            var snapshot = workspace.ReadSnapshot();
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
            workspace.ApplyIteration(iteration, response);
            UpdateLoopState(state, response);
            workspace.SaveLoopState(state);

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
            || string.IsNullOrWhiteSpace(snapshot.UserResponses))
        {
            return;
        }

        if (!snapshot.UserResponses.Contains(template.StopKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        state.StopKeywordMatched = true;
        response.Status = "done";
        response.DoneReason = $"The configured stop keyword '{template.StopKeyword}' was found in user responses.";
    }

    private static void UpdateLoopState(LoopState state, LoopIterationResponse response)
    {
        state.Phase = response.IsDone ? "done" : "active";
        state.OpenQuestions = ExtractNumberedItems(response.NextSteps);
        state.Decisions = ExtractBulletItems(response.CurrentState, "- Decision:");
        state.LastProcessedUserResponseAt = DateTimeOffset.UtcNow.ToString("O");
    }

    private static List<string> ExtractNumberedItems(string content)
    {
        return NumberedStepRegex.Matches(content)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static List<string> ExtractBulletItems(string content, string prefix)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[prefix.Length..].Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }
}