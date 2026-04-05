using Avalonia;
using Klavier.Audio;
using Klavier.Core.Engine;
using Klavier.Core.Options;
using Klavier.Core.Ports;
using Klavier.UI;
using Klavier.UI.Options;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        IConfiguration configuration = context.Configuration;

        services.Configure<PlaybackConfig>(configuration.GetSection("Playback"));
        services.Configure<UIConfig>(configuration.GetSection("UI"));
        services.AddFluidSynthAudio(configuration.GetSection("Audio"));
        services.AddSingleton<IPianoEngine, PianoEngine>();
        services.AddSingleton<PianoViewModel>();
        services.AddTransient<PianoView>();
        services.AddTransient<MainWindow>();
    })
    .Build();

// Initialize audio and register it as a note event handler
IAudioOutput audio = host.Services.GetRequiredService<IAudioOutput>();
audio.Initialize();

IPianoEngine engine = host.Services.GetRequiredService<IPianoEngine>();
engine.RegisterHandler(audio);

// Register UI as a note event handler (key highlighting)
PianoViewModel pianoViewModel = host.Services.GetRequiredService<PianoViewModel>();
engine.RegisterHandler(pianoViewModel);

AppBuilder.Configure(() => new App(() => host.Services.GetRequiredService<MainWindow>()))
    .UsePlatformDetect()
    .StartWithClassicDesktopLifetime(args);
