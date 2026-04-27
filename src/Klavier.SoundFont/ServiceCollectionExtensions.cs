using Klavier.SoundFont.Loading;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.SoundFont;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoundFont(this IServiceCollection services)
    {
        services.AddSingleton<ISoundFontFileLoader, SoundFontFileLoader>();
        return services;
    }
}
