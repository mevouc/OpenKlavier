using Klavier.UI.Ports;
using Klavier.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Klavier.Audio;
using Klavier.Core;
using Klavier.UI;
using Klavier.Midi;

namespace Klavier.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureAppServices(this IHostBuilder builder)
    {
        return builder.ConfigureServices((context, services) =>
        {
            IConfiguration configuration = context.Configuration;

            services.AddPianoEngine(configuration);
            services.AddFluidSynthAudio(configuration);
            services.AddMidi(configuration);
            services.AddSingleton<MidiPlaybackCoordinator>();
            services.AddUI(configuration);
        });
    }

    public static IHostBuilder UseUserSettings(this IHostBuilder builder, string appName)
    {
        return builder
            .ConfigureAppConfiguration((_, config) =>
            {
                UserSettingsService.EnsureCreated(appName);

                config.AddJsonFile(
                    UserSettingsService.GetFilePath(appName),
                    optional: true,
                    reloadOnChange: true);
            })
            .ConfigureServices((_, services) =>
                services.AddSingleton<IUserSettingsService>(new UserSettingsService(appName)));
    }
}
