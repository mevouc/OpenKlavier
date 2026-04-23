using Klavier.Config;
using Klavier.Core.Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klavier.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPianoEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PianoConfig>(configuration.GetSection(PianoConfig.SectionName));

        services.AddSingleton<IPianoEngine, PianoEngine>();

        return services;
    }
}
