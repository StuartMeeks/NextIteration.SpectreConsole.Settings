using NextIteration.SpectreConsole.Settings.Commands;

using Spectre.Console.Cli;

namespace NextIteration.SpectreConsole.Settings
{
    /// <summary>
    /// Spectre.Console.Cli configurator extensions for registering the
    /// settings-management command branch in a CLI.
    /// </summary>
    public static class CommandConfiguratorExtensions
    {
        /// <summary>
        /// Registers the <c>settings</c> branch of settings-management commands
        /// (<c>list</c>, <c>reset</c>).
        /// </summary>
        public static IConfigurator AddSettingsBranch(this IConfigurator configurator)
        {
            ArgumentNullException.ThrowIfNull(configurator);

            configurator.AddBranch("settings", settings =>
            {
                settings.SetDescription("Settings management commands");

                settings.AddCommand<ListSettingsCommand>("list")
                    .WithDescription("List all registered settings and their current values")
                    .WithExample("settings", "list");

                settings.AddCommand<ResetSettingsCommand>("reset")
                    .WithDescription("Reset a settings class (or all) to default values")
                    .WithExample("settings", "reset", "AppSettings")
                    .WithExample("settings", "reset", "--all");
            });

            return configurator;
        }
    }
}
