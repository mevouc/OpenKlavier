using Klavier.Config.Schema;
using Klavier.Midi.Loading;
using Klavier.Midi.Parsing;
using Klavier.Midi.Playback;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.Midi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMidi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PlayerConfig>(configuration.GetSection(PlayerConfig.SectionName));

        services.AddSingleton<IMidiScoreLoader, DryWetMidiScoreLoader>();
        services.AddSingleton<IMidiPlayer, MidiPlayer>();
        services.AddSingleton<IMidiFileLoader, MidiFileLoader>();
        services.AddSingleton<MidiPlaybackCoordinator>();

        return services;
    }
}
