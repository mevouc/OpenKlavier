using Klavier.Config.UserSettings;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserSettings(this IServiceCollection services, string appName)
    {
        services.AddSingleton<IUserSettingsService>(new UserSettingsService(appName));
        return services;
    }
}
