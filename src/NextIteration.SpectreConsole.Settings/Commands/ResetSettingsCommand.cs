using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

namespace NextIteration.SpectreConsole.Settings.Commands
{
    /// <summary>
    /// Spectre.Console command for <c>settings reset</c>. Resets a single
    /// settings class to defaults, or every registered class with
    /// <c>--all</c>, then persists.
    /// </summary>
    /// <remarks>DI constructor.</remarks>
    public sealed class ResetSettingsCommand(ISettingsStore store) : AsyncCommand<ResetSettingsCommand.Settings>
    {
        private readonly ISettingsStore _store = store;

        /// <summary>CLI settings for <c>settings reset</c>.</summary>
        public sealed class Settings : SettingsCommandSettings
        {
            /// <summary>
            /// Name of the settings class to reset (the simple type name, e.g.
            /// <c>AppSettings</c>). Omit when using <see cref="All"/>.
            /// </summary>
            [CommandArgument(0, "[SETTINGS_CLASS]")]
            [Description("The settings class to reset to defaults")]
            public string? SettingsClass { get; set; }

            /// <summary>Reset every registered settings class.</summary>
            [CommandOption("--all")]
            [Description("Reset all registered settings classes")]
            public bool All { get; set; }

            /// <summary>Skip the confirmation prompt. Useful in scripts.</summary>
            [CommandOption("-f|--force")]
            [Description("Reset without confirmation")]
            public bool Force { get; set; }
        }

        /// <inheritdoc />
        protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            try
            {
                if (settings.All)
                {
                    if (!string.IsNullOrWhiteSpace(settings.SettingsClass))
                    {
                        AnsiConsole.MarkupLine("[red]Specify either a settings class or --all, not both.[/]");
                        return 1;
                    }

                    if (_store.Registrations.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No settings classes are registered.[/]");
                        return 0;
                    }

                    if (!await ConfirmAsync(
                        settings,
                        $"Reset all {_store.Registrations.Count} settings class(es) to defaults? This overwrites their saved files and cannot be undone.",
                        cancellationToken).ConfigureAwait(false))
                    {
                        AnsiConsole.MarkupLine("[yellow]Reset cancelled.[/]");
                        return 0;
                    }

                    await _store.ResetAllAsync(cancellationToken).ConfigureAwait(false);
                    AnsiConsole.MarkupLine($"[green]Reset all {_store.Registrations.Count} settings class(es) to defaults.[/]");
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(settings.SettingsClass))
                {
                    AnsiConsole.MarkupLine("[red]Specify a settings class to reset, or pass --all.[/]");
                    RenderAvailableClasses();
                    return 1;
                }

                var registration = _store.Registrations
                    .FirstOrDefault(r => string.Equals(r.Name, settings.SettingsClass, StringComparison.OrdinalIgnoreCase));

                if (registration is null)
                {
                    AnsiConsole.MarkupLine($"[red]Unknown settings class '{Markup.Escape(settings.SettingsClass)}'.[/]");
                    RenderAvailableClasses();
                    return 1;
                }

                if (!await ConfirmAsync(
                    settings,
                    $"Reset '{Markup.Escape(registration.Name)}' to defaults? This overwrites the saved file and cannot be undone.",
                    cancellationToken).ConfigureAwait(false))
                {
                    AnsiConsole.MarkupLine("[yellow]Reset cancelled.[/]");
                    return 0;
                }

                await _store.ResetAsync(registration.SettingsType, cancellationToken).ConfigureAwait(false);
                AnsiConsole.MarkupLine($"[green]Reset '{Markup.Escape(registration.Name)}' to defaults.[/]");
                return 0;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Top-level boundary: turn any operational failure into a clean
                // message and a non-zero exit code rather than an unhandled
                // stack trace. A process-fatal OutOfMemoryException is left to
                // propagate.
                CommandErrorReporter.Report(ex, "Error resetting settings", settings.Verbose);
                return 1;
            }
        }

        // Returns true when the reset should proceed: either --force was passed
        // or the user confirmed. The prompt defaults to "no" since a reset is
        // destructive.
        private static async Task<bool> ConfirmAsync(Settings settings, string message, CancellationToken cancellationToken)
        {
            if (settings.Force)
            {
                return true;
            }

            return await AnsiConsole.ConfirmAsync(message, defaultValue: false, cancellationToken).ConfigureAwait(false);
        }

        private void RenderAvailableClasses()
        {
            if (_store.Registrations.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No settings classes are registered.[/]");
                return;
            }

            var names = string.Join(", ", _store.Registrations.Select(r => r.Name));
            AnsiConsole.MarkupLine($"[grey]Available: {Markup.Escape(names)}[/]");
        }
    }
}
