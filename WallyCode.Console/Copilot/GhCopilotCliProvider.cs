using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Copilot;

internal sealed class GhCopilotCliProvider : ILlmProvider
{
    private readonly AppLogger _logger;

    public GhCopilotCliProvider(string name, string defaultModel, string description, AppLogger logger)
    {
        Name = name;
        DefaultModel = defaultModel;
        Description = description;
        _logger = logger;
    }

    public string Name { get; }

    public string Description { get; }

    public string DefaultModel { get; }

    public async Task<string?> GetReadinessErrorAsync(CancellationToken cancellationToken)
    {
        var ghError = await CheckCommandAsync(
            ["--version"],
            $"{Name} requires GitHub CLI (`gh`) to be installed and available on PATH.",
            cancellationToken);

        if (ghError is not null)
        {
            return ghError;
        }

        var copilotError = await CheckCommandAsync(
            ["copilot", "--help"],
            $"{Name} requires the `gh copilot` command to be available.",
            cancellationToken);

        if (copilotError is not null)
        {
            return copilotError;
        }

        return await CheckCommandAsync(
            ["auth", "status"],
            $"{Name} requires GitHub CLI authentication. Run `gh auth login`.",
            cancellationToken);
    }

    public async Task<string> ExecuteAsync(CopilotRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(request));
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? DefaultModel
            : request.Model.Trim();
        var arguments = new List<string>
        {
            "copilot",
            "--model",
            model
        };

        string? workingDirectory = null;

        if (!string.IsNullOrWhiteSpace(request.SourcePath))
        {
            var sourcePath = Path.GetFullPath(request.SourcePath);

            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Source path does not exist: {sourcePath}");
            }

            arguments.Add("--add-dir");
            arguments.Add(sourcePath);
            workingDirectory = sourcePath;
        }

        arguments.Add("--yolo");
        arguments.Add("-s");
        arguments.Add("-p");
        arguments.Add(request.Prompt);

        _logger.Info($"Launching {Name} with model {model}.");
        var result = await GhProcess.RunAsync(arguments, workingDirectory, cancellationToken);

        if (result.ExitCode != 0)
        {
            var errorDetail = GhProcess.ErrorDetail(result);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorDetail)
                    ? $"gh copilot exited with code {result.ExitCode}."
                    : errorDetail);
        }

        return result.StandardOutput;
    }

    private static async Task<string?> CheckCommandAsync(
        IReadOnlyList<string> arguments,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await GhProcess.RunAsync(arguments, cancellationToken);

            if (result.Success)
            {
                return null;
            }

            var errorDetail = GhProcess.ErrorDetail(result);
            return string.IsNullOrWhiteSpace(errorDetail)
                ? failureMessage
                : $"{failureMessage} {errorDetail}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return $"{failureMessage} {exception.Message}";
        }
    }
}
