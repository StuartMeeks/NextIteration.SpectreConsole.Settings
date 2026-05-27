namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// Crash-safe text file writer. Writes to a uniquely-named temp file in the
    /// same directory as the final path, then performs an atomic rename.
    /// <see cref="File.Move(string, string, bool)"/> is atomic on NTFS and
    /// backed by <c>rename(2)</c> on POSIX, so a reader observes either the old
    /// content or the new content — never a partial write, even if the process
    /// is killed mid-call.
    /// </summary>
    /// <remarks>
    /// This does not serialise concurrent writers. Two writers each producing a
    /// new version observe "last-rename-wins" semantics — adequate for the
    /// interactive CLI usage this library targets.
    /// </remarks>
    internal static class AtomicFile
    {
        internal static async Task WriteAllTextAsync(
            string path,
            string contents,
            CancellationToken cancellationToken = default)
        {
            var tempPath = BuildTempPath(path);
            try
            {
                await File.WriteAllTextAsync(tempPath, contents, cancellationToken).ConfigureAwait(false);
                File.Move(tempPath, path, overwrite: true);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }

        private static string BuildTempPath(string finalPath) =>
            // Unique per call so concurrent writers don't collide on a shared
            // "{path}.tmp" name.
            $"{finalPath}.{Guid.NewGuid():N}.tmp";

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup; a stray ".tmp" file is harmless — it
                // doesn't match the "{ClassName}.json" name the loader reads.
            }
        }
    }
}
