using Avalonia;
using Klavier.Audio;
using Klavier.Core.Engine;
using Klavier.Config;
using Klavier.Core.Ports;
using Klavier.Extensions;
using Klavier.Services;
using Klavier.UI;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Klavier.UI.Theme;

const string AppName = "Klavier";

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseUserSettings(AppName)
    .ConfigureServices((context, services) =>
    {
        IConfiguration configuration = context.Configuration;

        services.Configure<PianoConfig>(configuration.GetSection(PianoConfig.SectionName));
        services.AddSingleton<IPianoEngine, PianoEngine>();
        services.AddFluidSynthAudio(configuration.GetSection(AudioConfig.SectionName));
        services.AddKlavierUI(configuration.GetSection(UIConfig.SectionName));
    })
    .Build();

// Heal any user-settings values that point at missing files or otherwise-invalid state.
// Must run before any consumer (e.g. PianoViewModel) reads KeyboardLayout / SoundFont.Path.
StartupConfigValidator.ValidateAndHeal(
    host.Services,
    host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Klavier.StartupConfigValidator"));

// Initialize audio and register it as a note event handler
IAudioOutput audio = host.Services.GetRequiredService<IAudioOutput>();
audio.Initialize();

IPianoEngine engine = host.Services.GetRequiredService<IPianoEngine>();
engine.RegisterHandler(audio);

// Register UI as a note event handler (key highlighting)
PianoViewModel pianoViewModel = host.Services.GetRequiredService<PianoViewModel>();
engine.RegisterHandler(pianoViewModel);

// Set active theme and load user colors before any views are created
UIConfig uiConfig = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<UIConfig>>().Value;
ThemePaletteProvider.SetActive(uiConfig.Theme switch
{
    AppTheme.Light => ThemePaletteProvider.Light,
    _ => ThemePaletteProvider.Dark,
});
UserPalette.Initialize(uiConfig.Colors);

try
{
    AppBuilder.Configure(() => new App(() => host.Services.GetRequiredService<MainWindow>()))
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args);
}
catch (Exception e)
{
    ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

    logger.LogError(e, "Unhandled exception: {Message}", e.Message);

    return 1;
}

return 0;
