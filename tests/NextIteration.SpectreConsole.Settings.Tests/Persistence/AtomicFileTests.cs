using NextIteration.SpectreConsole.Settings.Persistence;
using NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

using Xunit;

namespace NextIteration.SpectreConsole.Settings.Tests.Persistence
{
    public sealed class AtomicFileTests
    {
        [Fact]
        public async Task WriteAllTextAsync_WritesExpectedContent()
        {
            using var temp = new TempDir();
            var target = Path.Combine(temp.Path, "file.txt");

            await AtomicFile.WriteAllTextAsync(target, "hello", TestContext.Current.CancellationToken);

            Assert.Equal("hello", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task WriteAllTextAsync_NoTempFileLeftBehindAfterSuccess()
        {
            using var temp = new TempDir();
            var target = Path.Combine(temp.Path, "file.txt");

            await AtomicFile.WriteAllTextAsync(target, "hello", TestContext.Current.CancellationToken);

            var file = Assert.Single(Directory.GetFiles(temp.Path));
            Assert.Equal(target, file);
        }

        [Fact]
        public async Task WriteAllTextAsync_OverwritesExisting()
        {
            using var temp = new TempDir();
            var target = Path.Combine(temp.Path, "file.txt");
            await File.WriteAllTextAsync(target, "original", TestContext.Current.CancellationToken);

            await AtomicFile.WriteAllTextAsync(target, "replaced", TestContext.Current.CancellationToken);

            Assert.Equal("replaced", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task WriteAllTextAsync_ConcurrentWriters_OneWinsNoStragglers()
        {
            using var temp = new TempDir();
            var target = Path.Combine(temp.Path, "file.txt");

            await Task.WhenAll(
                AtomicFile.WriteAllTextAsync(target, "writer-a", TestContext.Current.CancellationToken),
                AtomicFile.WriteAllTextAsync(target, "writer-b", TestContext.Current.CancellationToken));

            var final = await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken);
            Assert.True(final is "writer-a" or "writer-b", $"expected one writer to win, got: {final}");

            // Unique temp names mean no leftover ".tmp" files.
            Assert.Single(Directory.GetFiles(temp.Path));
        }
    }
}
