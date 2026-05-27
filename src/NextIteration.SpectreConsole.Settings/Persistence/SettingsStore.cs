using System.Reflection;
using System.Text.Json;

namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// Default <see cref="ISettingsStore"/>. Holds the descriptor for every
    /// registered settings class, loads each instance lazily on first access
    /// (from disk, or a default when the file is absent), binds it to its
    /// persister, and caches it. The per-class DI singletons resolve through
    /// this store, so an in-place reset is observed by any command that
    /// injected the instance directly.
    /// </summary>
    internal sealed class SettingsStore : ISettingsStore
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<Type, SettingsTypeDescriptor> _descriptors;
        private readonly Dictionary<Type, Entry> _entries = new();
        private readonly List<SettingsRegistration> _registrations;

        public SettingsStore(IEnumerable<SettingsTypeDescriptor> descriptors)
        {
            ArgumentNullException.ThrowIfNull(descriptors);

            var ordered = descriptors.ToList();
            _descriptors = ordered.ToDictionary(d => d.SettingsType);
            _registrations = ordered
                .Select(d => new SettingsRegistration
                {
                    Name = d.Name,
                    SettingsType = d.SettingsType,
                    FilePath = d.FilePath,
                    PersistenceMode = d.PersistenceMode,
                })
                .ToList();
        }

        public IReadOnlyList<SettingsRegistration> Registrations => _registrations;

        public SettingsBase GetInstance(Type settingsType)
        {
            ArgumentNullException.ThrowIfNull(settingsType);

            lock (_gate)
            {
                return GetOrLoadLocked(settingsType).Instance;
            }
        }

        public async Task ResetAsync(Type settingsType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settingsType);

            Entry entry;
            SettingsTypeDescriptor descriptor;
            lock (_gate)
            {
                descriptor = GetDescriptorLocked(settingsType);
                entry = GetOrLoadLocked(settingsType);
            }

            ResetInstanceToDefaults(entry.Instance, descriptor);
            await entry.Persister.PersistAsync(entry.Instance, cancellationToken).ConfigureAwait(false);
        }

        public async Task ResetAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var registration in _registrations)
            {
                await ResetAsync(registration.SettingsType, cancellationToken).ConfigureAwait(false);
            }
        }

        private Entry GetOrLoadLocked(Type settingsType)
        {
            if (_entries.TryGetValue(settingsType, out var existing))
            {
                return existing;
            }

            var descriptor = GetDescriptorLocked(settingsType);
            var instance = Load(descriptor);
            var persister = new JsonSettingsPersister(descriptor.FilePath, descriptor.SettingsType, descriptor.SerializerOptions);

            // Bind only after deserialization, so the setter calls the
            // deserializer makes never schedule a write.
            instance.Bind(persister, descriptor.PersistenceMode, descriptor.ErrorHandler, descriptor.DebounceInterval);

            var entry = new Entry(instance, persister);
            _entries[settingsType] = entry;
            return entry;
        }

        private SettingsTypeDescriptor GetDescriptorLocked(Type settingsType)
        {
            if (!_descriptors.TryGetValue(settingsType, out var descriptor))
            {
                throw new ArgumentException(
                    $"Settings type '{settingsType.FullName}' was not registered. Call AddSettings<{settingsType.Name}>() during startup.",
                    nameof(settingsType));
            }

            return descriptor;
        }

        private static SettingsBase Load(SettingsTypeDescriptor descriptor)
        {
            try
            {
                if (File.Exists(descriptor.FilePath))
                {
                    var json = File.ReadAllText(descriptor.FilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var loaded = (SettingsBase?)JsonSerializer.Deserialize(json, descriptor.SettingsType, descriptor.SerializerOptions);
                        if (loaded is not null)
                        {
                            return loaded;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                // A malformed file shouldn't crash CLI startup. Surface it via
                // the configured handler, then fall back to defaults. (Missing
                // files are the common case and don't reach here.)
                descriptor.ErrorHandler(ex);
            }

            return descriptor.Factory();
        }

        private static void ResetInstanceToDefaults(SettingsBase instance, SettingsTypeDescriptor descriptor)
        {
            var defaults = descriptor.Factory();

            // Suspend notifications so the batch of setter calls produces no
            // intermediate writes; ResetAsync issues a single write afterwards.
            instance.SuspendNotifications();
            try
            {
                foreach (var property in descriptor.SettingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                    {
                        property.SetValue(instance, property.GetValue(defaults));
                    }
                }
            }
            finally
            {
                instance.ResumeNotifications();
            }
        }

        private sealed record Entry(SettingsBase Instance, ISettingsPersister Persister);
    }
}
