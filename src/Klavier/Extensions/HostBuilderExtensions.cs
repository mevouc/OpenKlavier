using Klavier.UI.Ports;
using Klavier.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            .ConfigureServices((_, services) =>
                services.AddSingleton<IUserSettingsService>(new UserSettingsService(appName)));
    }
}
