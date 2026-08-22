using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NextIteration.SpectreConsole.Settings.Persistence;

namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// DI extensions for registering strongly-typed, JSON-persisted settings
    /// classes from <c>NextIteration.SpectreConsole.Settings</c>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a single settings class. The instance is loaded from
        /// <c>{SettingsDirectory}/{typeof(T).Name}.json</c> on first use (or
        /// constructed from defaults when the file is absent) and registered as
        /// a singleton — inject <typeparamref name="T"/> directly into any
        /// command. Call once per settings class.
        /// </summary>
        /// <typeparam name="T">
        /// The settings class. Must derive from <see cref="SettingsBase"/> and
        /// have a public parameterless constructor (used both for defaults and
        /// by the JSON deserializer).
        /// </typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">
        /// Configures <see cref="SettingsOptions"/>.
        /// <see cref="SettingsOptions.SettingsDirectory"/> is required.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="SettingsOptions.SettingsDirectory"/> was not supplied.
        /// </exception>
        public static IServiceCollection AddSettings<T>(
            this IServiceCollection services,
            Action<SettingsOptions> configure)
            where T : SettingsBase, new()
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new SettingsOptions();
            configure(options);

            if (string.IsNullOrWhiteSpace(options.SettingsDirectory))
            {
                throw new InvalidOperationException(
                    $"{nameof(SettingsOptions)}.{nameof(SettingsOptions.SettingsDirectory)} must be set when registering settings class '{typeof(T).Name}'.");
            }

            var name = typeof(T).Name;
            var descriptor = new SettingsTypeDescriptor
            {
                SettingsType = typeof(T),
                Name = name,
                FilePath = Path.Join(options.SettingsDirectory, name + ".json"),
                PersistenceMode = options.PersistenceMode,
                DebounceInterval = options.DebounceInterval,
                ErrorHandler = options.ErrorHandler ?? SettingsSerialization.DefaultErrorHandler,
                SerializerOptions = options.SerializerOptions ?? SettingsSerialization.CreateDefaultOptions(),
                Factory = static () => new T(),
            };

            services.AddSingleton(descriptor);
            services.TryAddSingleton<ISettingsStore, SettingsStore>();

            // Resolve T through the store so the singleton instance is shared
            // with the store — a reset mutates the same object the consumer
            // injected.
            services.AddSingleton(sp => (T)sp.GetRequiredService<ISettingsStore>().GetInstance(typeof(T)));

            return services;
        }
    }
}
