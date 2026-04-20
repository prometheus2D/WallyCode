using WallyCode.ConsoleApp;
using WallyCode.ConsoleApp.Project;
using WallyCode.Tests.TestInfrastructure;

namespace WallyCode.Tests.Commands;

[Collection("Console")]
public class SetupCommandHandlerTests
{
    [Fact]
    public async Task Setup_creates_default_files_in_the_app_directory()
    {
        using var install = TempWorkspace.Create();

        var (exitCode, output) = await ExecuteAsync(["setup"], install.RootPath);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(ProjectSettings.GetFilePath(install.RootPath)));
        Assert.True(Directory.Exists(Path.Combine(install.RootPath, ".wallycode")));

        var settings = ProjectSettings.Load(install.RootPath);
        Assert.Equal("gh-copilot-claude", settings.Provider);
        Assert.Equal("claude-sonnet-4", settings.Model);

        Assert.Contains("Next commands:", output);
        Assert.Contains($"cd {install.RootPath}", output);
        Assert.Contains("wallycode provider", output);
    }

    [Fact]
    public async Task Setup_second_run_is_a_no_op_when_setup_already_exists()
    {
        using var install = TempWorkspace.Create();

        var (firstExitCode, _) = await ExecuteAsync(["setup"], install.RootPath);
        var initialSettingsJson = File.ReadAllText(ProjectSettings.GetFilePath(install.RootPath));

        var (secondExitCode, secondOutput) = await ExecuteAsync(["setup"], install.RootPath);
        var finalSettingsJson = File.ReadAllText(ProjectSettings.GetFilePath(install.RootPath));

        Assert.Equal(0, firstExitCode);
        Assert.Equal(0, secondExitCode);
        Assert.Equal(initialSettingsJson, finalSettingsJson);
        Assert.Contains("Setup already in place.", secondOutput);
    }

    [Fact]
    public async Task Setup_directory_override_creates_setup_in_the_provided_directory()
    {
        using var install = TempWorkspace.Create();
        using var temp = TempWorkspace.Create();
        var targetDirectory = Path.Combine(temp.RootPath, "workspace", "nested");

        var (exitCode, output) = await ExecuteAsync(["setup", "--directory", targetDirectory], install.RootPath);

        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(targetDirectory));
        Assert.True(File.Exists(ProjectSettings.GetFilePath(targetDirectory)));
        Assert.True(Directory.Exists(Path.Combine(targetDirectory, ".wallycode")));
        Assert.False(File.Exists(ProjectSettings.GetFilePath(install.RootPath)));
        Assert.False(Directory.Exists(Path.Combine(install.RootPath, ".wallycode")));
        Assert.Contains($"cd {targetDirectory}", output);
    }

    [Fact]
    public async Task Setup_without_force_preserves_existing_files()
    {
        using var install = TempWorkspace.Create();

        var existingSettings = new ProjectSettings
        {
            Provider = "gh-copilot-gpt5",
            Model = "gpt-5"
        };
        existingSettings.Save(install.RootPath);

        var runtimeDirectoryPath = Path.Combine(install.RootPath, ".wallycode");
        Directory.CreateDirectory(runtimeDirectoryPath);
        var markerPath = Path.Combine(runtimeDirectoryPath, "marker.txt");
        File.WriteAllText(markerPath, "keep");

        var (exitCode, output) = await ExecuteAsync(["setup"], install.RootPath);

        Assert.Equal(0, exitCode);
        Assert.Contains("Setup already in place.", output);
        Assert.True(File.Exists(markerPath));

        var settings = ProjectSettings.Load(install.RootPath);
        Assert.Equal("gh-copilot-gpt5", settings.Provider);
        Assert.Equal("gpt-5", settings.Model);
    }

    [Fact]
    public async Task Setup_force_recreates_existing_files_with_defaults()
    {
        using var install = TempWorkspace.Create();

        var existingSettings = new ProjectSettings
        {
            Provider = "gh-copilot-gpt5",
            Model = "gpt-5"
        };
        existingSettings.Save(install.RootPath);

        var runtimeDirectoryPath = Path.Combine(install.RootPath, ".wallycode");
        Directory.CreateDirectory(runtimeDirectoryPath);
        var markerPath = Path.Combine(runtimeDirectoryPath, "marker.txt");
        File.WriteAllText(markerPath, "remove");

        var (exitCode, output) = await ExecuteAsync(["setup", "--force"], install.RootPath);

        Assert.Equal(0, exitCode);
        Assert.Contains("Fresh setup complete.", output);
        Assert.False(File.Exists(markerPath));
        Assert.True(Directory.Exists(runtimeDirectoryPath));

        var settings = ProjectSettings.Load(install.RootPath);
        Assert.Equal("gh-copilot-claude", settings.Provider);
        Assert.Equal("claude-sonnet-4", settings.Model);
    }

    [Fact]
    public async Task Setup_vs_build_resolves_to_the_workspace_marker_above_the_build_output()
    {
        using var workspace = TempWorkspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.RootPath, ".git"));
        File.WriteAllText(Path.Combine(workspace.RootPath, "WallyCode.sln"), "Microsoft Visual Studio Solution File");

        var appDirectoryPath = Path.Combine(workspace.RootPath, "WallyCode.Console", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(appDirectoryPath);

        var (exitCode, output) = await ExecuteAsync(["setup", "--vs-build"], appDirectoryPath);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(ProjectSettings.GetFilePath(workspace.RootPath)));
        Assert.True(Directory.Exists(Path.Combine(workspace.RootPath, ".wallycode")));
        Assert.False(File.Exists(ProjectSettings.GetFilePath(appDirectoryPath)));
        Assert.Contains($"cd {workspace.RootPath}", output);
    }

    private static async Task<(int ExitCode, string Output)> ExecuteAsync(string[] args, string appDirectoryPath)
    {
        var writer = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(writer);
            var exitCode = await Program.RunAsync(args, CancellationToken.None, appDirectoryPath);
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}