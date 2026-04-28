using Klavier.SoundFont.Loading;
using Klavier.SoundFont.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.SoundFont;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoundFont(this IServiceCollection services)
    {
        services.AddSingleton<SoundFontInfoCache>();
        services.AddSingleton<ISoundFontInfoProvider>(sp => sp.GetRequiredService<SoundFontInfoCache>());
        services.AddSingleton<ISoundFontFileLoader, SoundFontFileLoader>();
        return services;
    }
}
