using System.Text.Json;

namespace WallyCode.ConsoleApp.Sessions;

internal static class SessionJson
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
