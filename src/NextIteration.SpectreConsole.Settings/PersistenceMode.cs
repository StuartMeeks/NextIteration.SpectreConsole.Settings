namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// Controls when a <see cref="SettingsBase"/> instance is written back to
    /// disk after its properties change.
    /// </summary>
    public enum PersistenceMode
    {
        /// <summary>
        /// The default. Each <see cref="SettingsBase.OnPropertyChanged(string?)"/>
        /// call schedules a debounced asynchronous write — a burst of property
        /// changes in the same synchronous call stack coalesces into a single
        /// disk write.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// <see cref="SettingsBase.OnPropertyChanged(string?)"/> is a no-op;
        /// nothing is persisted until the consumer calls
        /// <see cref="SettingsBase.Save"/> or
        /// <see cref="SettingsBase.SaveAsync(System.Threading.CancellationToken)"/>.
        /// </summary>
        Explicit = 1,
    }
}
