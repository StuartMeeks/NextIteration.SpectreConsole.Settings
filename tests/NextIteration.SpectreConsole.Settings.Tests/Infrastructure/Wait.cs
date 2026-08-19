using System.Diagnostics;

namespace NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

/// <summary>
/// Waits for an observable effect to appear, rather than sleeping for a fixed
/// interval and hoping it was long enough.
/// </summary>
/// <remarks>
/// Automatic persistence is debounced and fire-and-forget: nothing the caller can
/// await signals that the write finished, so a test can only observe its effect.
/// A fixed sleep has to be sized for the slowest machine that will ever run it,
/// and still fails spuriously when a loaded CI agent overruns the guess. Polling
/// returns as soon as the effect lands and only spends the whole budget on the
/// failure path, where the wait is a genuine timeout rather than dead time.
///
/// Negative assertions ("nothing was written") can't be polled — there is no
/// effect to wait for — so those keep an explicit delay. That direction is safe:
/// too short a wait can only let a bug through, never fail a correct build.
/// </remarks>
internal static class Wait
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Generous upper bound — reached only when the effect never happens, so it
    /// costs nothing on a passing run and gives a loaded agent ample headroom.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Polls <paramref name="condition"/> until it holds, or fails the test with
    /// <paramref name="expectation"/> in the message once the budget runs out.
    /// </summary>
    public static async Task UntilAsync(
        Func<bool> condition,
        string expectation,
        CancellationToken cancellationToken)
    {
        var elapsed = Stopwatch.StartNew();

        while (!condition())
        {
            if (elapsed.Elapsed > _timeout)
            {
                throw new TimeoutException(
                    $"Timed out after {_timeout.TotalSeconds:0}s waiting for {expectation}.");
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Polls until <paramref name="path"/> exists and contains
    /// <paramref name="substring"/>.
    /// </summary>
    public static Task UntilFileContainsAsync(
        string path,
        string substring,
        CancellationToken cancellationToken) =>
        UntilAsync(
            () => TryReadAllText(path)?.Contains(substring, StringComparison.Ordinal) == true,
            $"\"{substring}\" to appear in {path}",
            cancellationToken);

    // Writes land via a temp file and a rename, so a read can arrive mid-swap.
    // A missing or momentarily unreadable file just means "not yet" — poll again.
    private static string? TryReadAllText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
