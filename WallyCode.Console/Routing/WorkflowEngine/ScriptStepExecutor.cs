using System.Diagnostics;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Workflow;

internal sealed class ScriptStepExecutor : IStepExecutor
{
    private readonly AppLogger? _logger;

    public ScriptStepExecutor(AppLogger? logger = null)
    {
        _logger = logger;
    }

    public string ExecutionKind => StepExecutionKind.Script;

    public async Task<StepExecutionResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
    {
        var step = context.Step;
        if (string.IsNullOrWhiteSpace(step.ScriptPath))
        {
            throw new InvalidOperationException($"Step '{step.Name}' uses executionKind 'script' but has no scriptPath.");
        }

        var scriptPath = Path.IsPathRooted(step.ScriptPath)
            ? step.ScriptPath
            : Path.Combine(context.Session.SourcePath, step.ScriptPath);

        if (!File.Exists(scriptPath))
        {
            throw new InvalidOperationException($"Step '{step.Name}' script not found: {scriptPath}");
        }

        var timeout = TimeSpan.FromSeconds(step.TimeoutSeconds is > 0 ? step.TimeoutSeconds.Value : 120);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        var startInfo = new ProcessStartInfo
        {
            FileName = scriptPath,
            Arguments = step.ScriptArguments ?? string.Empty,
            WorkingDirectory = context.Session.SourcePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        _logger?.LogAction("Script step starting", $"step={step.Name}; script={scriptPath}; timeoutSeconds={timeout.TotalSeconds}");
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(timeoutSource.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeoutSource.Token);
        await process.WaitForExitAsync(timeoutSource.Token);
        var stdout = await outputTask;
        var stderr = await errorTask;
        var summary = $"Script exited with code {process.ExitCode}.";
        var combinedOutput = string.Join(Environment.NewLine, new[] { stdout.Trim(), stderr.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)));

        var memoryUpdates = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (step.WritesMemory.Count == 1)
        {
            memoryUpdates[step.WritesMemory[0]] = combinedOutput;
        }

        if (process.ExitCode != 0)
        {
            memoryUpdates[$"{step.Name}.exitCode"] = process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return new StepExecutionResult
        {
            Summary = string.IsNullOrWhiteSpace(combinedOutput) ? summary : $"{summary} {combinedOutput}",
            MemoryUpdates = memoryUpdates
        };
    }
}