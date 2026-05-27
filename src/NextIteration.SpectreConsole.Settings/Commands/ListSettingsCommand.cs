using System.Globalization;
using System.Reflection;

using Spectre.Console;
using Spectre.Console.Cli;

namespace NextIteration.SpectreConsole.Settings.Commands
{
    /// <summary>
    /// Spectre.Console command for <c>settings list</c>. Renders every
    /// registered settings class and its current property values, one table
    /// per class.
    /// </summary>
    /// <remarks>DI constructor.</remarks>
    public sealed class ListSettingsCommand(ISettingsStore store) : AsyncCommand<ListSettingsCommand.Settings>
    {
        private readonly ISettingsStore _store = store;

        /// <summary>CLI settings for <c>settings list</c>.</summary>
        public sealed class Settings : SettingsCommandSettings
        {
        }

        /// <inheritdoc />
        protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            try
            {
                if (_store.Registrations.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No settings classes are registered.[/]");
                    return Task.FromResult(0);
                }

                foreach (var registration in _store.Registrations)
                {
                    RenderRegistration(registration);
                    AnsiConsole.WriteLine();
                }

                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                CommandErrorReporter.Report(ex, "Error listing settings", settings.Verbose);
                return Task.FromResult(1);
            }
        }

        private void RenderRegistration(SettingsRegistration registration)
        {
            var instance = _store.GetInstance(registration.SettingsType);

            AnsiConsole.MarkupLine(
                $"[bold]{Markup.Escape(registration.Name)}[/] [grey]({registration.PersistenceMode})[/]");
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(registration.FilePath)}[/]");

            var table = new Table();
            _ = table.AddColumn("Property");
            _ = table.AddColumn("Value");

            var properties = registration.SettingsType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            if (properties.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey](no public properties)[/]");
                return;
            }

            foreach (var property in properties)
            {
                _ = table.AddRow(
                    Markup.Escape(property.Name),
                    Markup.Escape(FormatValue(property, instance)));
            }

            _ = table.Expand();
            AnsiConsole.Write(table);
        }

        private static string FormatValue(PropertyInfo property, object instance)
        {
            object? value;
            try
            {
                value = property.GetValue(instance);
            }
            catch (TargetInvocationException ex)
            {
                // A consumer getter that throws shouldn't take down `settings list`.
                return $"<error: {ex.InnerException?.Message ?? ex.Message}>";
            }

            return value switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            };
        }
    }
}
