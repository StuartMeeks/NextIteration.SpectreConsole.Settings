using NextIteration.SpectreConsole.Settings.Persistence;
using NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

using Xunit;

namespace NextIteration.SpectreConsole.Settings.Tests.Persistence;

public sealed class AtomicFileTests
{
    [Fact]
    public async Task WriteAllTextAsync_WritesExpectedContent()
    {
        using var temp = new TempDir();
        var target = Path.Combine(temp.Path, "file.txt");

        await AtomicFile.WriteAllTextAsync(target, "hello");

        Assert.Equal("hello", await File.ReadAllTextAsync(target));
    }

    [Fact]
    public async Task WriteAllTextAsync_NoTempFileLeftBehindAfterSuccess()
    {
        using var temp = new TempDir();
        var target = Path.Combine(temp.Path, "file.txt");

        await AtomicFile.WriteAllTextAsync(target, "hello");

        var files = Directory.GetFiles(temp.Path);
        Assert.Single(files);
        Assert.Equal(target, files[0]);
    }

    [Fact]
    public async Task WriteAllTextAsync_OverwritesExisting()
    {
        using var temp = new TempDir();
        var target = Path.Combine(temp.Path, "file.txt");
        await File.WriteAllTextAsync(target, "original");

        await AtomicFile.WriteAllTextAsync(target, "replaced");

        Assert.Equal("replaced", await File.ReadAllTextAsync(target));
    }

    [Fact]
    public async Task WriteAllTextAsync_ConcurrentWriters_OneWinsNoStragglers()
    {
        using var temp = new TempDir();
        var target = Path.Combine(temp.Path, "file.txt");

        await Task.WhenAll(
            AtomicFile.WriteAllTextAsync(target, "writer-a"),
            AtomicFile.WriteAllTextAsync(target, "writer-b"));

        var final = await File.ReadAllTextAsync(target);
        Assert.True(final is "writer-a" or "writer-b", $"expected one writer to win, got: {final}");

        // Unique temp names mean no leftover ".tmp" files.
        Assert.Single(Directory.GetFiles(temp.Path));
    }
}
