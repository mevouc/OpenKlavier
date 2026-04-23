using Klavier.Config;
using Klavier.Midi.DryWetMidi;
using Klavier.Midi.Player;
using Klavier.Midi.Ports;
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

        return services;
    }
}
