using Microsoft.Extensions.DependencyInjection;

using NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

using Xunit;

namespace NextIteration.SpectreConsole.Settings.Tests;

public sealed class SettingsStoreTests
{
    private static readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(40);

    // Negative assertions only — see the note on Wait. Positive ones poll.
    private static readonly TimeSpan _quiet = TimeSpan.FromMilliseconds(400);

    private static ServiceProvider BuildProvider(string directory, PersistenceMode mode = PersistenceMode.Automatic) =>
        new ServiceCollection()
            .AddSettings<SampleSettings>(options =>
            {
                options.SettingsDirectory = directory;
                options.PersistenceMode = mode;
                options.DebounceInterval = _debounce;
            })
            .BuildServiceProvider();

    private static string FileFor<T>(string directory) =>
        Path.Combine(directory, typeof(T).Name + ".json");

    [Fact]
    public void AddSettings_WithoutSettingsDirectory_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddSettings<SampleSettings>(_ => { }));

        Assert.Contains(nameof(SettingsOptions.SettingsDirectory), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingFile_ResolvesDefaults_WithoutCreatingFile()
    {
        using var temp = new TempDir();
        using var provider = BuildProvider(temp.Path);

        var settings = provider.GetRequiredService<SampleSettings>();

        Assert.Equal("default-name", settings.Name);
        Assert.Equal(1, settings.Count);
        Assert.Equal(SampleMode.First, settings.Mode);
        // Loading a missing file must not write one — only a mutation does.
        Assert.False(File.Exists(FileFor<SampleSettings>(temp.Path)));
    }

    [Fact]
    public void Resolved_Instance_IsSameAsStoreInstance()
    {
        using var temp = new TempDir();
        using var provider = BuildProvider(temp.Path);

        var injected = provider.GetRequiredService<SampleSettings>();
        var fromStore = provider.GetRequiredService<ISettingsStore>().GetInstance(typeof(SampleSettings));

        Assert.Same(injected, fromStore);
    }

    [Fact]
    public void Registration_FileIsNamedAfterClass()
    {
        using var temp = new TempDir();
        using var provider = BuildProvider(temp.Path);

        var registration = Assert.Single(provider.GetRequiredService<ISettingsStore>().Registrations);

        Assert.Equal("SampleSettings", registration.Name);
        Assert.Equal(FileFor<SampleSettings>(temp.Path), registration.FilePath);
    }

    [Fact]
    public async Task Automatic_Mutation_PersistsAndRoundTrips()
    {
        using var temp = new TempDir();

        await using (var provider = BuildProvider(temp.Path))
        {
            var settings = provider.GetRequiredService<SampleSettings>();
            settings.Name = "persisted";
            settings.Mode = SampleMode.Second;

            // The debounced write is fire-and-forget with nothing to await, and
            // disposing the provider does not flush it — so wait for the value to
            // actually reach disk before tearing this scope down and reloading.
            await Wait.UntilFileContainsAsync(
                FileFor<SampleSettings>(temp.Path),
                "persisted",
                TestContext.Current.CancellationToken);
        }

        // A fresh provider over the same directory must observe the writes.
        await using var reloadedProvider = BuildProvider(temp.Path);
        var reloaded = reloadedProvider.GetRequiredService<SampleSettings>();

        Assert.Equal("persisted", reloaded.Name);
        Assert.Equal(SampleMode.Second, reloaded.Mode);
    }

    [Fact]
    public async Task Explicit_DoesNotWriteUntilSave()
    {
        using var temp = new TempDir();
        var file = FileFor<SampleSettings>(temp.Path);

        await using var provider = BuildProvider(temp.Path, PersistenceMode.Explicit);
        var settings = provider.GetRequiredService<SampleSettings>();

        settings.Name = "changed";
        await Task.Delay(_quiet, TestContext.Current.CancellationToken);
        Assert.False(File.Exists(file));

        await settings.SaveAsync(TestContext.Current.CancellationToken);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task TolerantDeserialization_MissingDefaulted_UnknownIgnored()
    {
        using var temp = new TempDir();

        // Case-insensitive key, one known property, no Count/Mode, plus an
        // unknown property the schema has never heard of.
        await File.WriteAllTextAsync(
            FileFor<SampleSettings>(temp.Path),
            """{ "name": "fromfile", "removedLegacyProperty": 42 }""",
            TestContext.Current.CancellationToken);

        await using var provider = BuildProvider(temp.Path);
        var settings = provider.GetRequiredService<SampleSettings>();

        Assert.Equal("fromfile", settings.Name);          // present (case-insensitive)
        Assert.Equal(1, settings.Count);                  // missing -> default
        Assert.Equal(SampleMode.First, settings.Mode);    // missing -> default
    }

    [Fact]
    public async Task CorruptFile_BacksUpAndFallsBackToDefaults()
    {
        using var temp = new TempDir();
        var file = FileFor<SampleSettings>(temp.Path);
        const string corrupt = "{ this is not valid json ";
        await File.WriteAllTextAsync(file, corrupt, TestContext.Current.CancellationToken);

        Exception? surfaced = null;
        await using var provider = new ServiceCollection()
            .AddSettings<SampleSettings>(o =>
            {
                o.SettingsDirectory = temp.Path;
                o.ErrorHandler = ex => surfaced = ex; // no-op stderr, capture instead
            })
            .BuildServiceProvider();

        var settings = provider.GetRequiredService<SampleSettings>();

        // Falls back to defaults rather than throwing on startup...
        Assert.Equal("default-name", settings.Name);
        // ...the parse error is surfaced...
        Assert.IsType<System.Text.Json.JsonException>(surfaced);
        // ...and the unreadable content is preserved as a sidecar.
        var backup = file + ".bak";
        Assert.True(File.Exists(backup));
        Assert.Equal(corrupt, await File.ReadAllTextAsync(backup, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetAsync_RestoresDefaults_InPlaceAndOnDisk()
    {
        using var temp = new TempDir();

        await using var provider = BuildProvider(temp.Path);
        var store = provider.GetRequiredService<ISettingsStore>();
        var settings = provider.GetRequiredService<SampleSettings>();

        settings.Name = "changed";
        settings.Count = 99;
        await settings.SaveAsync(TestContext.Current.CancellationToken);

        await store.ResetAsync(typeof(SampleSettings), TestContext.Current.CancellationToken);

        // The live instance the consumer holds is reset in place.
        Assert.Equal("default-name", settings.Name);
        Assert.Equal(1, settings.Count);

        // And the defaults are persisted: a fresh load sees them too.
        await using var reloadedProvider = BuildProvider(temp.Path);
        var reloaded = reloadedProvider.GetRequiredService<SampleSettings>();
        Assert.Equal("default-name", reloaded.Name);
        Assert.Equal(1, reloaded.Count);
    }

    [Fact]
    public async Task ResetAllAsync_RestoresEveryRegisteredClass()
    {
        using var temp = new TempDir();

        await using var provider = new ServiceCollection()
            .AddSettings<SampleSettings>(o => o.SettingsDirectory = temp.Path)
            .AddSettings<SecondarySettings>(o => o.SettingsDirectory = temp.Path)
            .BuildServiceProvider();

        var sample = provider.GetRequiredService<SampleSettings>();
        var secondary = provider.GetRequiredService<SecondarySettings>();
        sample.Name = "changed";
        secondary.Enabled = false;

        await provider.GetRequiredService<ISettingsStore>().ResetAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal("default-name", sample.Name);
        Assert.True(secondary.Enabled);
    }

    [Fact]
    public async Task ResetAsync_UnregisteredType_Throws()
    {
        using var temp = new TempDir();
        await using var provider = BuildProvider(temp.Path);
        var store = provider.GetRequiredService<ISettingsStore>();

        await Assert.ThrowsAsync<ArgumentException>(() => store.ResetAsync(typeof(SecondarySettings), TestContext.Current.CancellationToken));
    }
}
