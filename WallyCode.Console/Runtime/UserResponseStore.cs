using System.Text.Json;

namespace WallyCode.ConsoleApp.Runtime;

internal sealed class UserResponseStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<UserResponseEntry> Responses { get; set; } = [];
}
