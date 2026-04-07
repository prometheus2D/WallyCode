using System.Text.Json;

namespace WallyCode.ConsoleApp.Loop;

internal static class LoopTemplateRegistry
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LoopTemplate Load(string? templateId)
    {
        var resolvedTemplateId = string.IsNullOrWhiteSpace(templateId)
            ? "default"
            : templateId.Trim();

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Loops", $"{resolvedTemplateId}.json");

        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException($"Loop template '{resolvedTemplateId}' was not found at {templatePath}.");
        }

        var template = JsonSerializer.Deserialize<LoopTemplate>(File.ReadAllText(templatePath), SerializerOptions)
            ?? throw new InvalidOperationException($"Loop template '{resolvedTemplateId}' is invalid.");

        template.TemplateId = resolvedTemplateId;
        template.Validate(resolvedTemplateId);
        return template;
    }
}
