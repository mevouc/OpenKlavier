using Klavier.Audio;
using Klavier.Config;
using Klavier.Config.UserSettings;
using Klavier.Core;
using Klavier.Midi;
using Klavier.SoundFont;
using Klavier.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Klavier.Extensions;

public static class HostBuilderExtensions
{
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
            .ConfigureServices((_, services) => services.AddUserSettings(appName));
    }

    public static IHostBuilder ConfigureAppServices(this IHostBuilder builder)
    {
        return builder.ConfigureServices((context, services) =>
        {
            IConfiguration configuration = context.Configuration;

            services.AddPianoEngine(configuration);
            services.AddFluidSynthAudio(configuration);
            services.AddMidi(configuration);
            services.AddSoundFont();
            services.AddUI(configuration);
        });
    }
}
