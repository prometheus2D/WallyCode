using System.Text.Json.Serialization;

namespace WallyCode.ConsoleApp.Loop;

internal sealed class LoopTemplate
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string SystemPrompt { get; set; } = string.Empty;

    public string ResponseSchemaPrompt { get; set; } = string.Empty;

    public string InitialCurrentTasks { get; set; } = string.Empty;

    public string InitialPerspectives { get; set; } = string.Empty;

    public string InitialNextSteps { get; set; } = string.Empty;

    public string InitialCurrentState { get; set; } = string.Empty;

    public string? StopKeyword { get; set; }

    [JsonIgnore]
    public string TemplateId { get; set; } = string.Empty;

    public void Validate(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException("A loop template id is required.");
        }

        if (string.IsNullOrWhiteSpace(SystemPrompt))
        {
            throw new InvalidOperationException($"Loop template '{templateId}' is missing systemPrompt.");
        }

        if (string.IsNullOrWhiteSpace(ResponseSchemaPrompt))
        {
            throw new InvalidOperationException($"Loop template '{templateId}' is missing responseSchemaPrompt.");
        }
    }
}
