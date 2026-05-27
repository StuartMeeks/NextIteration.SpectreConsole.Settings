using System.Text.Json;

namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// <see cref="ISettingsPersister"/> backed by a single JSON file. Created
    /// once per settings instance at load time, capturing that instance's file
    /// path, concrete type, and serializer options.
    /// </summary>
    internal sealed class JsonSettingsPersister : ISettingsPersister
    {
        private readonly string _filePath;
        private readonly Type _settingsType;
        private readonly JsonSerializerOptions _serializerOptions;

        internal JsonSettingsPersister(string filePath, Type settingsType, JsonSerializerOptions serializerOptions)
        {
            _filePath = filePath;
            _settingsType = settingsType;
            _serializerOptions = serializerOptions;
        }

        public async Task PersistAsync(SettingsBase settings, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            // The directory may not exist on the very first write (settings is
            // created from defaults when the file is absent).
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, _settingsType, _serializerOptions);
            await AtomicFile.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
    }
}
