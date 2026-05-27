namespace NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

/// <summary>
/// Throwaway directory under the system temp path. Tests that need a settings
/// directory (or any disk scratch space) should wrap this in <c>using</c> so
/// the directory is recursively removed when the test ends regardless of
/// pass/fail.
/// </summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ni.scs.tests." + Guid.NewGuid().ToString("N"));

    public TempDir()
    {
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup. Stray scratch dirs in %TEMP% aren't a
            // problem — the OS reclaims temp eventually.
        }
    }
}
