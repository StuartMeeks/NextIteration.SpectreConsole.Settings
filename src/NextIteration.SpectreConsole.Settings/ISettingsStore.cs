namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// Aggregates every settings class registered via <c>AddSettings&lt;T&gt;</c>.
    /// Resolves and caches the live singleton instance for each class, and
    /// powers the <c>settings list</c> / <c>settings reset</c> commands.
    /// Registered as a singleton; the per-class singletons resolve through it
    /// so a reset is observed by any command that injected the instance
    /// directly.
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>All registered settings classes, in registration order.</summary>
        IReadOnlyList<SettingsRegistration> Registrations { get; }

        /// <summary>
        /// Returns the live singleton instance for <paramref name="settingsType"/>,
        /// loading it from disk (or constructing a default) on first access.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="settingsType"/> was never registered.
        /// </exception>
        SettingsBase GetInstance(Type settingsType);

        /// <summary>
        /// Resets a single settings class to default values in place and
        /// persists the result immediately. The reset is applied to the live
        /// instance, so callers holding a reference observe the change.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="settingsType"/> was never registered.
        /// </exception>
        Task ResetAsync(Type settingsType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets every registered settings class to default values and
        /// persists each one immediately.
        /// </summary>
        Task ResetAllAsync(CancellationToken cancellationToken = default);
    }
}
