using Microsoft.Extensions.DependencyInjection;

using NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

using Xunit;

namespace NextIteration.SpectreConsole.Settings.Tests;

/// <summary>
/// End-to-end tests of the <c>settings</c> command branch through a real
/// <c>CommandApp</c>, with scripted prompt input.
/// </summary>
public sealed class CommandFlowTests
{
    private static string FileFor<T>(string directory) =>
        Path.Combine(directory, typeof(T).Name + ".json");

    private static void Register(string directory, IServiceCollection services) =>
        services.AddSettings<SampleSettings>(o => o.SettingsDirectory = directory);

    // Writes a non-default settings file so a reset has something to undo.
    private static Task SeedChangedAsync(string directory) =>
        File.WriteAllTextAsync(
            FileFor<SampleSettings>(directory),
            "{\"Name\":\"changed\",\"Count\":99,\"Mode\":\"Second\"}",
            TestContext.Current.CancellationToken);

    private static string LoadedName(string directory)
    {
        using var provider = new ServiceCollection()
            .AddSettings<SampleSettings>(o => o.SettingsDirectory = directory)
            .BuildServiceProvider();
        return provider.GetRequiredService<SampleSettings>().Name;
    }

    [Fact]
    public async Task List_RendersRegisteredClassAndValues()
    {
        using var temp = new TempDir();
        await SeedChangedAsync(temp.Path);

        var result = await CliHarness.RunAsync(
            services => Register(temp.Path, services),
            ["settings", "list"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("SampleSettings", result.Output, StringComparison.Ordinal);
        Assert.Contains("changed", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reset_Declined_DoesNotReset()
    {
        using var temp = new TempDir();
        await SeedChangedAsync(temp.Path);

        var result = await CliHarness.RunAsync(
            services => Register(temp.Path, services),
            ["settings", "reset", "SampleSettings"],
            consoleInput: "n");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("cancelled", result.Output, StringComparison.OrdinalIgnoreCase);
        // The file is untouched — still the seeded non-default value.
        Assert.Equal("changed", LoadedName(temp.Path));
    }

    [Fact]
    public async Task Reset_Confirmed_Resets()
    {
        using var temp = new TempDir();
        await SeedChangedAsync(temp.Path);

        var result = await CliHarness.RunAsync(
            services => Register(temp.Path, services),
            ["settings", "reset", "SampleSettings"],
            consoleInput: "y");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("default-name", LoadedName(temp.Path));
    }

    [Fact]
    public async Task Reset_Force_SkipsPromptAndResets()
    {
        using var temp = new TempDir();
        await SeedChangedAsync(temp.Path);

        // No console input pushed — if a prompt were shown it would hang/fail.
        var result = await CliHarness.RunAsync(
            services => Register(temp.Path, services),
            ["settings", "reset", "SampleSettings", "--force"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("default-name", LoadedName(temp.Path));
    }

    [Fact]
    public async Task Reset_UnknownClass_ReportsAndListsAvailable()
    {
        using var temp = new TempDir();

        var result = await CliHarness.RunAsync(
            services => Register(temp.Path, services),
            ["settings", "reset", "NoSuchSettings", "--force"]);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown settings class", result.Output, StringComparison.Ordinal);
        Assert.Contains("SampleSettings", result.Output, StringComparison.Ordinal);
    }
}
