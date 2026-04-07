using System.Text;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Loop;

internal static class LoopMemoryRenderer
{
    public static string RenderCurrentTasks(LoopState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Current Tasks");
        builder.AppendLine();

        if (state.Phase == "done")
        {
            builder.AppendLine("1. The loop is complete.");
            return builder.ToString().TrimEnd();
        }

        if (state.OpenQuestions.Count > 0)
        {
            builder.AppendLine("1. Resolve the highest-priority open question.");
            builder.AppendLine("2. Incorporate any new user responses.");
            builder.AppendLine("3. Re-evaluate whether the loop can finish.");
            return builder.ToString().TrimEnd();
        }

        builder.AppendLine("1. Review the latest summary and determine the next bounded step.");
        builder.AppendLine("2. Continue the loop if more work remains.");
        return builder.ToString().TrimEnd();
    }

    public static string RenderNextSteps(LoopState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Next Steps");
        builder.AppendLine();

        if (state.Phase == "done")
        {
            builder.AppendLine("1. No further action is required.");
            return builder.ToString().TrimEnd();
        }

        if (state.OpenQuestions.Count == 0)
        {
            builder.AppendLine("1. Run the next iteration.");
            return builder.ToString().TrimEnd();
        }

        for (var index = 0; index < state.OpenQuestions.Count; index++)
        {
            builder.AppendLine($"{index + 1}. {state.OpenQuestions[index]}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string RenderCurrentState(LoopSessionState session, LoopState state, LoopIterationResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Current State");
        builder.AppendLine();
        builder.AppendLine($"- Loop status: {response.Status}");
        builder.AppendLine($"- Phase: {state.Phase}");
        builder.AppendLine($"- Latest summary: {response.Summary}");
        builder.AppendLine($"- Next iteration: {session.NextIteration + 1}");
        builder.AppendLine($"- Open questions: {state.OpenQuestions.Count}");
        builder.AppendLine($"- Decisions tracked: {state.Decisions.Count}");
        builder.AppendLine($"- Stop keyword matched: {state.StopKeywordMatched}");

        if (!string.IsNullOrWhiteSpace(state.LastProcessedUserResponseAt))
        {
            builder.AppendLine($"- Last processed user response at: {state.LastProcessedUserResponseAt}");
        }

        if (state.Decisions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Decisions");
            builder.AppendLine();

            foreach (var decision in state.Decisions)
            {
                builder.AppendLine($"- Decision: {decision}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
