using System.Text.Json;

namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// Options passed to <c>AddSettings&lt;T&gt;</c> to configure how a single
    /// settings class is loaded from and persisted to disk.
    /// </summary>
    public sealed class SettingsOptions
    {
        /// <summary>
        /// Absolute path to the directory where settings JSON files are stored.
        /// <b>Required</b> — there is no smart default. Registration throws if
        /// this is left unset. Each settings class is written to its own file
        /// within this directory, named after the class (e.g.
        /// <c>AppSettings.json</c>).
        /// </summary>
        public string SettingsDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Whether property changes are persisted automatically (debounced) or
        /// only when the consumer explicitly calls <see cref="SettingsBase.Save"/>.
        /// Defaults to <see cref="PersistenceMode.Automatic"/>.
        /// </summary>
        public PersistenceMode PersistenceMode { get; set; } = PersistenceMode.Automatic;

        /// <summary>
        /// Invoked when an asynchronous (fire-and-forget) disk write fails, so
        /// that errors on the automatic-persistence path are surfaced rather
        /// than silently swallowed. When <see langword="null"/>, the library
        /// writes a single diagnostic line to <see cref="System.Console.Error"/>.
        /// </summary>
        public Action<Exception>? ErrorHandler { get; set; }

        /// <summary>
        /// How long to wait after the last property change before an automatic
        /// write is performed. A burst of changes within this window collapses
        /// into a single write. Defaults to 250&#160;ms. Ignored in
        /// <see cref="PersistenceMode.Explicit"/> mode.
        /// </summary>
        public TimeSpan DebounceInterval { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Optional <see cref="JsonSerializerOptions"/> override. When
        /// <see langword="null"/>, a tolerant default is used (indented output,
        /// case-insensitive property matching, missing properties fall back to
        /// defaults, unknown properties are ignored).
        /// </summary>
        public JsonSerializerOptions? SerializerOptions { get; set; }
    }
}
