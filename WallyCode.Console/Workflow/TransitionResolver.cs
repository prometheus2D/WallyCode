using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Workflow;

internal sealed class TransitionDecision
{
    public required string SelectedStep { get; init; }
    public required string NextStepName { get; init; }
    public required string Status { get; init; }
    public required bool StopsInvocation { get; init; }
}

internal sealed class TransitionResolver
{
    private const string AskUser = "ask_user";
    private const string Done = "done";
    private const string Error = "error";

    public TransitionDecision Resolve(WorkflowDefinition definition, WorkflowStep step, Session session, StepExecutionResult executionResult)
    {
        var guardedTransition = step.Transitions.FirstOrDefault(transition => transition.Guard is not null && GuardMatches(transition.Guard, session, executionResult));
        if (guardedTransition is not null)
        {
            return ToDecision(definition, step, guardedTransition, session, executionResult);
        }

        var selectedStep = NormalizeSelection(step, executionResult.SelectedStep ?? string.Empty);
        if (string.IsNullOrWhiteSpace(selectedStep))
        {
            throw new InvalidOperationException($"Step '{step.Name}' did not select a transition and no guarded transition matched.");
        }

        if (string.Equals(selectedStep, AskUser, StringComparison.Ordinal))
        {
            return new TransitionDecision { SelectedStep = AskUser, NextStepName = step.Name, Status = SessionStatus.Blocked, StopsInvocation = true };
        }

        if (string.Equals(selectedStep, Done, StringComparison.Ordinal))
        {
            return new TransitionDecision { SelectedStep = Done, NextStepName = step.Name, Status = SessionStatus.Completed, StopsInvocation = true };
        }

        if (string.Equals(selectedStep, Error, StringComparison.Ordinal))
        {
            return new TransitionDecision { SelectedStep = Error, NextStepName = step.Name, Status = SessionStatus.Error, StopsInvocation = true };
        }

        var transition = step.Transitions.FirstOrDefault(transition => string.Equals(transition.Selection, selectedStep, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Selected step '{selectedStep}' is not allowed for step '{step.Name}'.");

        if (transition.Guard is not null)
        {
            throw new InvalidOperationException($"Selected step '{selectedStep}' is guarded and its guard did not match for step '{step.Name}'.");
        }

        return ToDecision(definition, step, transition, session, executionResult);
    }

    private static TransitionDecision ToDecision(WorkflowDefinition definition, WorkflowStep step, WorkflowTransition transition, Session session, StepExecutionResult executionResult)
    {
        EnsureHandoffMemory(definition, step, transition, session, executionResult);

        return new TransitionDecision
        {
            SelectedStep = transition.Selection,
            NextStepName = transition.TargetStepName ?? step.Name,
            Status = transition.Status,
            StopsInvocation = transition.StopsInvocation
        };
    }

    private static void EnsureHandoffMemory(WorkflowDefinition definition, WorkflowStep step, WorkflowTransition transition, Session session, StepExecutionResult executionResult)
    {
        if (string.IsNullOrWhiteSpace(transition.TargetStepName)
            || string.Equals(transition.TargetStepName, step.Name, StringComparison.Ordinal))
        {
            return;
        }

        var targetStep = definition.GetStep(transition.TargetStepName);
        var handoffKeys = targetStep.ReadsMemory
            .Where(key => step.WritesMemory.Contains(key, StringComparer.Ordinal))
            .ToList();

        foreach (var key in handoffKeys)
        {
            if (!HasMemoryValue(session, executionResult, key))
            {
                throw new InvalidOperationException($"Transition '{transition.Selection}' from step '{step.Name}' requires memory key '{key}' before moving to step '{targetStep.Name}'.");
            }
        }
    }

    private static bool GuardMatches(WorkflowTransitionGuard? guard, Session session, StepExecutionResult executionResult)
    {
        if (guard is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(guard.SelectedStep)
            && !string.Equals(guard.SelectedStep, executionResult.SelectedStep, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var key in guard.MemoryExists)
        {
            if (!HasMemoryValue(session, executionResult, key))
            {
                return false;
            }
        }

        foreach (var key in guard.MemoryMissing)
        {
            if (HasMemoryValue(session, executionResult, key))
            {
                return false;
            }
        }

        foreach (var expected in guard.MemoryEquals)
        {
            if (!TryGetMemoryValue(session, executionResult, expected.Key, out var actual)
                || !string.Equals(actual, expected.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasMemoryValue(Session session, StepExecutionResult executionResult, string key)
    {
        return TryGetMemoryValue(session, executionResult, key, out var value) && !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetMemoryValue(Session session, StepExecutionResult executionResult, string key, out string? value)
    {
        if (executionResult.MemoryUpdates.TryGetValue(key, out var updatedValue))
        {
            value = updatedValue;
            return updatedValue is not null;
        }

        return session.Memory.TryGetValue(key, out value);
    }

    private static string NormalizeSelection(WorkflowStep step, string selectedStep)
    {
        return string.Equals(selectedStep, step.Name, StringComparison.Ordinal)
            && step.Transitions.Any(transition => string.Equals(transition.Selection, "continue", StringComparison.Ordinal))
            ? "continue"
            : selectedStep;
    }
}