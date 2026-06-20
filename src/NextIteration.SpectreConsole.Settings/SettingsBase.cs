using System.Runtime.CompilerServices;

using NextIteration.SpectreConsole.Settings.Persistence;

namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// Base class for consumer settings. Derive from it, back each public
    /// property with a field, and call <see cref="OnPropertyChanged(string?)"/>
    /// from the setter — that's the whole contract:
    /// <code>
    /// public sealed class AppSettings : SettingsBase
    /// {
    ///     private string _theme = "dark";
    ///     public string Theme
    ///     {
    ///         get => _theme;
    ///         set { _theme = value; OnPropertyChanged(); }
    ///     }
    /// }
    /// </code>
    /// In <see cref="PersistenceMode.Automatic"/> mode each change schedules a
    /// debounced asynchronous write; in <see cref="PersistenceMode.Explicit"/>
    /// mode the consumer calls <see cref="Save"/> / <see cref="SaveAsync"/>.
    /// </summary>
    /// <remarks>
    /// An instance is inert until the framework <see cref="Bind"/>s it during
    /// load. That means property assignments performed by the JSON deserializer
    /// (which run before binding) never trigger a write — only changes the
    /// consumer makes after startup do.
    /// </remarks>
    public abstract class SettingsBase
    {
        private readonly object _gate = new();

        private ISettingsPersister? _persister;
        private PersistenceMode _persistenceMode = PersistenceMode.Automatic;
        private Action<Exception> _errorHandler = static _ => { };
        private TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(250);

        // The CTS for the in-flight debounce window. A newer change (or an
        // explicit Save) cancels it so only the last write in a burst survives.
        private CancellationTokenSource? _debounceCts;

        // Re-entrant suspend counter. While > 0, OnPropertyChanged is a no-op —
        // used by an in-place reset that mutates many properties but wants a
        // single explicit write afterwards.
        private int _suspendDepth;

        /// <summary>
        /// Wires this instance to its backing store. Called once by the
        /// framework immediately after load, before the instance is handed to
        /// consumers. Idempotent re-binding is allowed (last binding wins).
        /// </summary>
        internal void Bind(
            ISettingsPersister persister,
            PersistenceMode persistenceMode,
            Action<Exception> errorHandler,
            TimeSpan debounceInterval)
        {
            ArgumentNullException.ThrowIfNull(persister);
            ArgumentNullException.ThrowIfNull(errorHandler);

            lock (_gate)
            {
                _persister = persister;
                _persistenceMode = persistenceMode;
                _errorHandler = errorHandler;
                _debounceInterval = debounceInterval;
            }
        }

        /// <summary>
        /// Call from a property setter after mutating the backing field. In
        /// <see cref="PersistenceMode.Automatic"/> mode this schedules a
        /// debounced write; in <see cref="PersistenceMode.Explicit"/> mode it
        /// does nothing.
        /// </summary>
        /// <param name="propertyName">
        /// Supplied automatically by the compiler via
        /// <see cref="CallerMemberNameAttribute"/>. Not used to decide what to
        /// persist (the whole object is always written) — accepted so derived
        /// types can pass it through and so future change-tracking can use it.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            lock (_gate)
            {
                if (_suspendDepth > 0)
                {
                    return;
                }

                if (_persistenceMode != PersistenceMode.Automatic || _persister is null)
                {
                    return;
                }

                ScheduleDebouncedWriteLocked(_persister);
            }
        }

        /// <summary>
        /// Persists the current state immediately (fire-and-forget). Valid in
        /// both persistence modes. Any pending debounced write is superseded.
        /// Errors are routed to the configured error handler rather than thrown
        /// on the caller's stack. Use <see cref="SaveAsync"/> when you need to
        /// await completion — e.g. before a CLI command returns and the process
        /// exits.
        /// </summary>
        public void Save()
        {
            ISettingsPersister? persister;
            lock (_gate)
            {
                CancelPendingWriteLocked();
                persister = _persister;
            }

            if (persister is null)
            {
                return;
            }

            _ = PersistGuardedAsync(persister);
        }

        /// <summary>
        /// Persists the current state immediately and returns a task that
        /// completes when the write finishes. Valid in both persistence modes.
        /// Any pending debounced write is superseded. Unlike <see cref="Save"/>,
        /// exceptions propagate to the awaiter.
        /// </summary>
        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            ISettingsPersister? persister;
            lock (_gate)
            {
                CancelPendingWriteLocked();
                persister = _persister;
            }

            return persister is null
                ? Task.CompletedTask
                : persister.PersistAsync(this, cancellationToken);
        }

        /// <summary>
        /// Suspends <see cref="OnPropertyChanged(string?)"/> writes until the
        /// matching <see cref="ResumeNotifications"/>. Re-entrant. Used by an
        /// in-place reset so a batch of property mutations does not each trigger
        /// a separate write.
        /// </summary>
        internal void SuspendNotifications()
        {
            lock (_gate)
            {
                _suspendDepth++;
            }
        }

        /// <summary>Counterpart to <see cref="SuspendNotifications"/>.</summary>
        internal void ResumeNotifications()
        {
            lock (_gate)
            {
                if (_suspendDepth > 0)
                {
                    _suspendDepth--;
                }
            }
        }

        private void ScheduleDebouncedWriteLocked(ISettingsPersister persister)
        {
            CancelPendingWriteLocked();

            var cts = new CancellationTokenSource();
            _debounceCts = cts;

            _ = DebounceAndPersistAsync(persister, _debounceInterval, cts);
        }

        private void CancelPendingWriteLocked()
        {
            if (_debounceCts is null)
            {
                return;
            }

            _debounceCts.Cancel();
            _debounceCts.Dispose();
            _debounceCts = null;
        }

        private async Task DebounceAndPersistAsync(
            ISettingsPersister persister,
            TimeSpan interval,
            CancellationTokenSource cts)
        {
            try
            {
                if (interval > TimeSpan.Zero)
                {
                    await Task.Delay(interval, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    // Let the current synchronous call stack unwind so a burst
                    // of setters all schedule-then-cancel and only the last
                    // survives to write — coalescing with a zero interval too.
                    await Task.Yield();
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                return; // superseded by a newer change or an explicit Save.
            }
            finally
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_debounceCts, cts))
                    {
                        _debounceCts = null;
                    }
                }

                cts.Dispose();
            }

            await PersistGuardedAsync(persister).ConfigureAwait(false);
        }

        private async Task PersistGuardedAsync(ISettingsPersister persister)
        {
            try
            {
                await persister.PersistAsync(this).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Never swallow: route to the configured handler (default
                // writes to stderr). This is the fire-and-forget safety net.
                _errorHandler(ex);
            }
        }
    }
}
