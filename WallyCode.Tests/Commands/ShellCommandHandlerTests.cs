using WallyCode.ConsoleApp.Commands;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Commands;

[Collection("Console")]
public class ShellCommandHandlerTests
{
    [Fact]
    public async Task Shell_vs_build_resolves_source_from_build_output_and_resets_memory_in_workspace_root()
    {
        using var workspace = TempWorkspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.RootPath, ".git"));
        File.WriteAllText(Path.Combine(workspace.RootPath, "WallyCode.sln"), "Microsoft Visual Studio Solution File");

        var appDirectoryPath = Path.Combine(workspace.RootPath, "WallyCode.Console", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(appDirectoryPath);

        var runtimeRoot = Path.Combine(workspace.RootPath, ".wallycode");
        Directory.CreateDirectory(runtimeRoot);
        File.WriteAllText(Path.Combine(runtimeRoot, "marker.txt"), "old");

        var options = new ShellCommandOptions
        {
            VsBuild = true,
            ResetMemory = true
        };

        var handler = new ShellCommandHandler(options, appDirectoryPath);
        var reader = new StringReader("exit" + Environment.NewLine);
        var writer = new StringWriter();
        var originalIn = Console.In;
        var originalOut = Console.Out;

        try
        {
            Console.SetIn(reader);
            Console.SetOut(writer);

            var exitCode = await handler.ExecuteAsync(CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.False(File.Exists(Path.Combine(runtimeRoot, "marker.txt")));
            Assert.Contains($"Shell initialized with source: {workspace.RootPath}", writer.ToString());
            Assert.Contains($"Reset session at {runtimeRoot}", writer.ToString());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }
}
