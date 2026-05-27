using System.Text.Json;

namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// Immutable registration record for one settings class. One descriptor is
    /// registered in DI per <c>AddSettings&lt;T&gt;</c> call; the
    /// <see cref="SettingsStore"/> receives them all and uses them to load,
    /// bind, and reset instances lazily.
    /// </summary>
    internal sealed class SettingsTypeDescriptor
    {
        public required Type SettingsType { get; init; }

        public required string Name { get; init; }

        public required string FilePath { get; init; }

        public required PersistenceMode PersistenceMode { get; init; }

        public required TimeSpan DebounceInterval { get; init; }

        public required Action<Exception> ErrorHandler { get; init; }

        public required JsonSerializerOptions SerializerOptions { get; init; }

        /// <summary>Constructs a fresh default instance (<c>new T()</c>).</summary>
        public required Func<SettingsBase> Factory { get; init; }
    }
}
