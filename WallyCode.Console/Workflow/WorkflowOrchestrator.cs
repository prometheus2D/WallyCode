using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;
using WallyCode.ConsoleApp.Sessions;

namespace WallyCode.ConsoleApp.Workflow;

internal sealed class WorkflowOrchestrator
{
    private const string Error = "error";

    private readonly WorkflowDefinition _definition;
    private readonly string _sessionRoot;
    private readonly IReadOnlyDictionary<string, IStepExecutor> _executors;
    private readonly TransitionResolver _transitionResolver;
    private readonly AppLogger? _logger;
    private readonly string _globalPrompt;

    public WorkflowOrchestrator(
        WorkflowDefinition definition,
        string sessionRoot,
        IEnumerable<IStepExecutor> executors,
        AppLogger? logger = null,
        TransitionResolver? transitionResolver = null)
    {
        _definition = definition;
        _sessionRoot = sessionRoot;
        _executors = executors.ToDictionary(executor => executor.ExecutionKind, StringComparer.OrdinalIgnoreCase);
        _transitionResolver = transitionResolver ?? new TransitionResolver();
        _logger = logger;
        _globalPrompt = LoadGlobalPrompt(sessionRoot);
    }

    public async Task<IterationResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var session = Session.Load(_sessionRoot);

        if (session.WorkflowName != _definition.Name)
        {
            throw new InvalidOperationException(
                $"Session is on workflow '{session.WorkflowName}' but '{_definition.Name}' was supplied.");
        }

        if (session.Status is SessionStatus.Completed or SessionStatus.Error)
        {
            throw new InvalidOperationException($"Session is {session.Status}; nothing to run.");
        }

        StepExecutionResult executionResult;
        TransitionDecision decision;

        try
        {
            var step = _definition.GetStep(session.ActiveStepName);
            var executor = ResolveExecutor(step);
            executionResult = await executor.ExecuteAsync(new StepExecutionContext
            {
                Definition = _definition,
                Step = step,
                Session = session,
                SessionRoot = _sessionRoot,
                GlobalPrompt = _globalPrompt
            }, cancellationToken);

            executionResult = FilterMemoryUpdates(step, executionResult);
            decision = _transitionResolver.Resolve(_definition, step, session, executionResult);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            session.IterationCount++;
            session.LastSelectedStep = Error;
            session.LastSummary = ex.Message;
            session.Status = SessionStatus.Error;
            session.PendingResponses.Clear();
            session.Save(_sessionRoot);
            session.SaveSnapshot(_sessionRoot);
            throw;
        }

        session.IterationCount++;
        session.LastSelectedStep = decision.SelectedStep;
        session.LastSummary = executionResult.Summary;
        ApplyMemoryUpdates(session, executionResult.MemoryUpdates);
        session.ActiveStepName = decision.NextStepName;
        session.Status = decision.Status;
        session.PendingResponses.Clear();
        session.Save(_sessionRoot);
        session.SaveSnapshot(_sessionRoot);

        return new IterationResult
        {
            IterationNumber = session.IterationCount,
            SelectedStep = decision.SelectedStep,
            Summary = executionResult.Summary,
            ActiveStepName = decision.NextStepName,
            Status = decision.Status,
            StopsInvocation = decision.StopsInvocation
        };
    }

    public async Task<IReadOnlyList<IterationResult>> RunAsync(int steps, CancellationToken cancellationToken)
    {
        var results = new List<IterationResult>();
        for (var i = 0; i < steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RunOnceAsync(cancellationToken);
            results.Add(result);
            if (result.StopsInvocation) break;
        }

        return results;
    }

    private IStepExecutor ResolveExecutor(WorkflowStep step)
    {
        return _executors.TryGetValue(step.ExecutionKind, out var executor)
            ? executor
            : throw new InvalidOperationException($"No step executor is registered for executionKind '{step.ExecutionKind}'.");
    }

    private StepExecutionResult FilterMemoryUpdates(WorkflowStep step, StepExecutionResult executionResult)
    {
        if (executionResult.MemoryUpdates.Count == 0)
        {
            return executionResult;
        }

        var allowed = new HashSet<string>(step.WritesMemory, StringComparer.Ordinal);
        var filtered = executionResult.MemoryUpdates
            .Where(update => allowed.Contains(update.Key))
            .ToDictionary(update => update.Key, update => update.Value, StringComparer.Ordinal);

        var ignored = executionResult.MemoryUpdates.Keys.Where(key => !allowed.Contains(key)).ToList();
        if (ignored.Count > 0)
        {
            _logger?.LogAction("Ignored undeclared memory updates", $"step={step.Name}; keys={string.Join(",", ignored)}");
        }

        return new StepExecutionResult
        {
            SelectedStep = executionResult.SelectedStep,
            Summary = executionResult.Summary,
            MemoryUpdates = filtered
        };
    }

    private static void ApplyMemoryUpdates(Session session, IReadOnlyDictionary<string, string?> memoryUpdates)
    {
        foreach (var update in memoryUpdates)
        {
            if (update.Value is null)
            {
                session.Memory.Remove(update.Key);
            }
            else
            {
                session.Memory[update.Key] = update.Value;
            }
        }
    }

    private static string LoadGlobalPrompt(string sessionRoot)
    {
        var projectRoot = Directory.GetParent(sessionRoot)?.FullName;
        return string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot)
            ? string.Empty
            : ProjectSettings.Load(projectRoot).GlobalPrompt ?? string.Empty;
    }
}