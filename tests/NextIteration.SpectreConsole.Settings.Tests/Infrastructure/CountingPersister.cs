using NextIteration.SpectreConsole.Settings.Persistence;

namespace NextIteration.SpectreConsole.Settings.Tests.Infrastructure
{
    /// <summary>
    /// Test double for <see cref="ISettingsPersister"/> that counts writes (and can
    /// optionally fail), letting <see cref="SettingsBase"/> debounce / explicit /
    /// error-surfacing behaviour be asserted deterministically without touching
    /// disk.
    /// </summary>
    internal sealed class CountingPersister : ISettingsPersister
    {
        private int _writeCount;
        private readonly bool _throwOnWrite;

        public CountingPersister(bool throwOnWrite = false)
        {
            _throwOnWrite = throwOnWrite;
        }

        public int WriteCount => Volatile.Read(ref _writeCount);

        public Task PersistAsync(SettingsBase settings, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _writeCount);
            return _throwOnWrite
                ? Task.FromException(new InvalidOperationException("write failed"))
                : Task.CompletedTask;
        }
    }
}
