using Microsoft.Extensions.DependencyInjection;

using NextIteration.SpectreConsole.Settings;

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace NextIteration.SpectreConsole.Settings.Tests.Infrastructure
{
    /// <summary>Captured result of running the CLI in a test.</summary>
    internal sealed record CliResult(int ExitCode, string Output);

    /// <summary>
    /// Drives the real <c>settings</c> command branch end-to-end: builds a
    /// <see cref="CommandApp"/> over a DI container (via <see cref="TypeRegistrar"/>),
    /// redirects the static <see cref="AnsiConsole"/> to a <see cref="TestConsole"/>
    /// so prompt input can be scripted and output captured, runs the given args,
    /// then restores the console.
    /// </summary>
    /// <remarks>
    /// The commands write to the static <c>AnsiConsole</c> rather than an injected
    /// console, so the swap is on the global. Each run restores it in a
    /// <c>finally</c>; only this harness touches the global, and xUnit serialises
    /// tests within a class, so there's no cross-test bleed.
    /// </remarks>
    internal static class CliHarness
    {
        public static async Task<CliResult> RunAsync(
            Action<IServiceCollection> configureServices,
            string[] args,
            params string[] consoleInput)
        {
            var services = new ServiceCollection();
            configureServices(services);

            var app = new CommandApp(new TypeRegistrar(services));
            app.Configure(config => config.AddSettingsBranch());

            using var console = new TestConsole();
            console.Interactive();
            foreach (var line in consoleInput)
            {
                console.Input.PushTextWithEnter(line);
            }

            var original = AnsiConsole.Console;
            AnsiConsole.Console = console;
            try
            {
                var exitCode = await app.RunAsync(args).ConfigureAwait(false);
                return new CliResult(exitCode, console.Output);
            }
            finally
            {
                AnsiConsole.Console = original;
            }
        }
    }

    /// <summary>
    /// Standard Spectre.Console.Cli <see cref="ITypeRegistrar"/> adapter over
    /// <see cref="IServiceCollection"/>, so DI-constructed commands (which take an
    /// <c>ISettingsStore</c>) resolve in tests exactly as they would in a host app.
    /// </summary>
    internal sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
    {
        public ITypeResolver Build() => new TypeResolver(builder.BuildServiceProvider());

        public void Register(Type service, Type implementation) => builder.AddSingleton(service, implementation);

        public void RegisterInstance(Type service, object implementation) => builder.AddSingleton(service, implementation);

        public void RegisterLazy(Type service, Func<object> factory) => builder.AddSingleton(service, _ => factory());
    }

    internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
    {
        public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

        public void Dispose()
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
