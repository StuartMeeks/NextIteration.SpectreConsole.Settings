namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// Writes a single <see cref="SettingsBase"/> instance back to its backing
    /// store. One persister is bound to each settings instance at load time;
    /// it captures the file path and serializer options for that instance so
    /// <see cref="SettingsBase"/> itself stays storage-agnostic.
    /// </summary>
    internal interface ISettingsPersister
    {
        /// <summary>
        /// Serialises <paramref name="settings"/> and writes it to disk.
        /// Implementations write atomically so a crash mid-write never leaves
        /// a half-written file.
        /// </summary>
        Task PersistAsync(SettingsBase settings, CancellationToken cancellationToken = default);
    }
}
