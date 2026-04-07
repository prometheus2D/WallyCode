namespace WallyCode.ConsoleApp.Runtime;

internal sealed class UserResponseEntry
{
    public int Id { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public string Text { get; set; } = string.Empty;
}
