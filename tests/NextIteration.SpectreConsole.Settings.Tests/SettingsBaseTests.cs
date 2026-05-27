using NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

using Xunit;

namespace NextIteration.SpectreConsole.Settings.Tests;

public sealed class SettingsBaseTests
{
    private static readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(40);

    // Comfortably longer than the debounce window so a scheduled write has
    // definitely fired (or definitely not, in the explicit/no-op cases).
    private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void OnPropertyChanged_WhenUnbound_DoesNotThrow()
    {
        var settings = new SampleSettings();

        // No Bind() has happened (mirrors the deserializer setting properties
        // during load) — the change must be a silent no-op, not a crash.
        var ex = Record.Exception(() => settings.Name = "changed");

        Assert.Null(ex);
    }

    [Fact]
    public async Task Automatic_SingleChange_PersistsOnce()
    {
        var settings = new SampleSettings();
        var persister = new CountingPersister();
        settings.Bind(persister, PersistenceMode.Automatic, static _ => { }, _debounce);

        settings.Name = "changed";
        await Task.Delay(_settle);

        Assert.Equal(1, persister.WriteCount);
    }

    [Fact]
    public async Task Automatic_BurstOfChanges_CoalescesIntoSingleWrite()
    {
        var settings = new SampleSettings();
        var persister = new CountingPersister();
        settings.Bind(persister, PersistenceMode.Automatic, static _ => { }, _debounce);

        // Three synchronous mutations in one call stack must collapse to one
        // disk write — each change supersedes the previous pending write.
        settings.Name = "a";
        settings.Count = 2;
        settings.Mode = SampleMode.Second;

        await Task.Delay(_settle);

        Assert.Equal(1, persister.WriteCount);
    }

    [Fact]
    public async Task Explicit_PropertyChange_DoesNotPersist()
    {
        var settings = new SampleSettings();
        var persister = new CountingPersister();
        settings.Bind(persister, PersistenceMode.Explicit, static _ => { }, _debounce);

        settings.Name = "changed";
        await Task.Delay(_settle);

        Assert.Equal(0, persister.WriteCount);
    }

    [Fact]
    public async Task Explicit_SaveAsync_Persists()
    {
        var settings = new SampleSettings();
        var persister = new CountingPersister();
        settings.Bind(persister, PersistenceMode.Explicit, static _ => { }, _debounce);

        settings.Name = "changed";
        await settings.SaveAsync();

        Assert.Equal(1, persister.WriteCount);
    }

    [Fact]
    public async Task Save_FireAndForget_EventuallyPersists()
    {
        var settings = new SampleSettings();
        var persister = new CountingPersister();
        settings.Bind(persister, PersistenceMode.Explicit, static _ => { }, _debounce);

        settings.Save();
        await Task.Delay(_settle);

        Assert.Equal(1, persister.WriteCount);
    }

    [Fact]
    public async Task Automatic_FailedWrite_SurfacesToErrorHandler()
    {
        Exception? captured = null;
        var settings = new SampleSettings();
        var persister = new CountingPersister(throwOnWrite: true);
        settings.Bind(persister, PersistenceMode.Automatic, ex => captured = ex, _debounce);

        settings.Name = "boom";
        await Task.Delay(_settle);

        Assert.NotNull(captured);
        Assert.IsType<InvalidOperationException>(captured);
    }

    [Fact]
    public async Task SaveAsync_FailedWrite_PropagatesToAwaiter()
    {
        var settings = new SampleSettings();
        var persister = new CountingPersister(throwOnWrite: true);
        settings.Bind(persister, PersistenceMode.Explicit, static _ => { }, _debounce);

        // SaveAsync is awaitable, so the error reaches the caller directly
        // rather than the fire-and-forget handler.
        await Assert.ThrowsAsync<InvalidOperationException>(() => settings.SaveAsync());
    }
}
