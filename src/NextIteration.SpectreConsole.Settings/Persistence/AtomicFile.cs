namespace NextIteration.SpectreConsole.Settings.Persistence
{
    /// <summary>
    /// Crash-safe text file writer. Writes to a uniquely-named temp file in the
    /// same directory as the final path, then atomically replaces the final path,
    /// so a reader observes either the old content or the new content — never a
    /// partial write, even if the process is killed mid-call.
    ///
    /// The replace primitive differs by platform. On POSIX,
    /// <see cref="File.Move(string, string, bool)"/> is <c>rename(2)</c>, which
    /// replaces the destination even while another handle holds it open, and
    /// serialises concurrent renames. On Windows the same call is
    /// <c>MoveFileEx</c> with <c>MOVEFILE_REPLACE_EXISTING</c>, which does
    /// <em>not</em> tolerate that: it raises a sharing violation when the
    /// destination is open or when two replacements race. Windows therefore uses
    /// <see cref="File.Replace(string, string, string)"/> (<c>ReplaceFile</c>),
    /// which is built for exactly that case, with a short retry for the window
    /// between testing for the destination and replacing it.
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
                await ReplaceAtomicallyAsync(tempPath, path).ConfigureAwait(false);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }

        /// <summary>
        /// Moves <paramref name="tempPath"/> onto <paramref name="path"/>,
        /// replacing it if present. See the type remarks for why Windows cannot
        /// use the POSIX path.
        /// </summary>
        private static async Task ReplaceAtomicallyAsync(string tempPath, string path)
        {
            if (!OperatingSystem.IsWindows())
            {
                File.Move(tempPath, path, overwrite: true);
                return;
            }

            const int maxAttempts = 5;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        // ReplaceFile semantics: tolerates an open destination and
                        // deletes the source on success. No backup file wanted.
                        File.Replace(tempPath, path, destinationBackupFileName: null);
                    }
                    else
                    {
                        // Destination absent, so a plain move is the whole job.
                        // Deliberately not overwrite:true — if a racing writer
                        // created it in the meantime we want the throw, and the
                        // retry below routes us to File.Replace instead.
                        File.Move(tempPath, path);
                    }

                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException && attempt < maxAttempts)
                {
                    // A concurrent writer is mid-replace, or created the
                    // destination between our File.Exists test and the call.
                    // Both are transient; back off and re-evaluate.
                    await Task.Delay(10 * attempt).ConfigureAwait(false);
                }
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
