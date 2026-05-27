using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// Shared serialization defaults and the fallback error handler used when a
    /// consumer doesn't supply their own.
    /// </summary>
    internal static class SettingsSerialization
    {
        /// <summary>
        /// Builds the tolerant default <see cref="JsonSerializerOptions"/>:
        /// indented output for human-readable files, case-insensitive property
        /// matching, enums written as strings, comments and trailing commas
        /// allowed on read. Missing properties fall back to their constructed
        /// defaults and unknown properties are ignored — both native
        /// <see cref="System.Text.Json"/> behaviours — which is what gives the
        /// store its schema-evolution tolerance.
        /// </summary>
        internal static JsonSerializerOptions CreateDefaultOptions() => new(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// Fallback error handler. Writes a single diagnostic line to
        /// <see cref="Console.Error"/> so a failed fire-and-forget write is
        /// never silently swallowed.
        /// </summary>
        internal static void DefaultErrorHandler(Exception exception) =>
            Console.Error.WriteLine($"[NextIteration.SpectreConsole.Settings] Failed to persist settings: {exception.Message}");
    }
}
