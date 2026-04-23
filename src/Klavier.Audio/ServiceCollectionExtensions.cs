using Klavier.Config;
using Klavier.Core.Ports;
using Klavier.SoundFont;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.Audio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluidSynthAudio(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AudioConfig>(configuration.GetSection(AudioConfig.SectionName));

        services.AddSingleton<FluidSynthAudioOutput>();
        services.AddSingleton<IAudioOutput>(sp => sp.GetRequiredService<FluidSynthAudioOutput>());
        services.AddSingleton<ISoundFontInfoProvider>(sp => sp.GetRequiredService<FluidSynthAudioOutput>());

        return services;
    }
}
