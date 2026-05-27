namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// Describes one settings class registered via <c>AddSettings&lt;T&gt;</c>.
    /// Surfaced by <see cref="ISettingsStore.Registrations"/> so commands such
    /// as <c>settings list</c> can enumerate every registered class and where
    /// it lives on disk without reflecting over the DI container.
    /// </summary>
    public sealed class SettingsRegistration
    {
        /// <summary>
        /// Display name of the settings class — the simple type name (e.g.
        /// <c>AppSettings</c>), which is also the JSON file's base name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>The concrete settings type, a subclass of <see cref="SettingsBase"/>.</summary>
        public required Type SettingsType { get; init; }

        /// <summary>Absolute path to this class's JSON file on disk.</summary>
        public required string FilePath { get; init; }

        /// <summary>The persistence mode configured for this class at registration time.</summary>
        public required PersistenceMode PersistenceMode { get; init; }
    }
}
