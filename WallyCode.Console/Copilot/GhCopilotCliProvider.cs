using System.Net.Http.Headers;
using System.Text.Json;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Copilot;

internal sealed class GhCopilotCliProvider : ILlmProvider
{
    private readonly AppLogger _logger;
    private static readonly Uri ModelsEndpoint = new("https://api.githubcopilot.com/models");

    public GhCopilotCliProvider(string name, string defaultModel, string description, AppLogger logger)
    {
        Name = name;
        DefaultModel = defaultModel;
        Description = description;
        SupportedModels = [defaultModel];
        _logger = logger;
    }

    public string Name { get; }

    public string Description { get; }

    public string DefaultModel { get; }

    public IReadOnlyList<string> SupportedModels { get; }

    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken)
    {
        var discoveredModels = new HashSet<string>(SupportedModels, StringComparer.OrdinalIgnoreCase);

        try
        {
            var tokenResult = await GhProcess.RunAsync(["auth", "token"], cancellationToken);

            if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.StandardOutput))
            {
                return discoveredModels.OrderBy(model => model, StringComparer.OrdinalIgnoreCase).ToList();
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.StandardOutput.Trim());
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WallyCode");

            using var response = await httpClient.GetAsync(ModelsEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var modelElement in dataElement.EnumerateArray())
                {
                    if (modelElement.TryGetProperty("id", out var idElement))
                    {
                        var modelId = idElement.GetString();

                        if (!string.IsNullOrWhiteSpace(modelId))
                        {
                            discoveredModels.Add(modelId.Trim());
                        }
                    }
                }
            }
        }
        catch
        {
        }

        return discoveredModels
            .OrderBy(model => model, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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

        if (string.IsNullOrWhiteSpace(GhProcess.TryResolveCopilotExecutablePath()))
        {
            return $"{Name} requires the `copilot` CLI to be installed on PATH or in the GitHub CLI managed install location.";
        }

        var copilotError = await CheckCopilotCommandAsync(
            ["--help"],
            $"{Name} requires the `copilot` CLI to be runnable.",
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
        var result = await GhProcess.RunCopilotAsync(arguments, workingDirectory, cancellationToken);

        if (result.ExitCode != 0)
        {
            var errorDetail = GhProcess.ErrorDetail(result);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorDetail)
                    ? $"copilot exited with code {result.ExitCode}."
                    : errorDetail);
        }

        return result.StandardOutput;
    }

    private static async Task<string?> CheckCopilotCommandAsync(
        IReadOnlyList<string> arguments,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await GhProcess.RunCopilotAsync(arguments, cancellationToken);

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
