namespace WallyCode.Tests.TestInfrastructure;

internal sealed class TempWorkspace : IDisposable
{
    public string RootPath { get; }

    private TempWorkspace(string rootPath)
    {
        RootPath = rootPath;
    }

    public static TempWorkspace Create()
    {
        var path = Path.Combine(Path.GetTempPath(), "wallycode-tests-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(path);
        return new TempWorkspace(path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }
}
