using Spectre.Console;

namespace NextIteration.SpectreConsole.Settings.Commands
{
    /// <summary>
    /// Centralised error rendering for the <c>settings</c> commands so every
    /// command's catch block behaves identically — a terse single-line message
    /// by default, the full exception when <c>--verbose</c> is set.
    /// </summary>
    internal static class CommandErrorReporter
    {
        /// <summary>
        /// Writes <paramref name="ex"/> to the console. In verbose mode the full
        /// exception view is rendered; otherwise a single coloured line prefixed
        /// with <paramref name="contextMessage"/>.
        /// </summary>
        internal static void Report(Exception ex, string contextMessage, bool verbose)
        {
            // Escape defensively: contextMessage is library-internal today, but
            // ex.Message is always external (JSON/IO errors commonly contain
            // '[' / ']' which Spectre would otherwise parse as markup).
            if (verbose)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(contextMessage)}[/]");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(contextMessage)}: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[grey]Run with --verbose for more detail.[/]");
            }
        }
    }
}
