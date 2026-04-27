using Avalonia;
using Klavier.Config.Schema;
using Klavier.Core.Engine;
using Klavier.Core.Ports;
using Klavier.Midi.Loading;
using Klavier.Midi.Playback;
using Klavier.Services;
using Klavier.UI;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.Extensions;

public static class HostExtensions
{
    public static IHost EnsureValidUserSettings(this IHost host)
    {
        // Heal any user-settings values that point at missing files or otherwise-invalid state.
        // Must run before any consumer (e.g. PianoViewModel) reads KeyboardLayout / SoundFont.Path.
        StartupConfigValidationService.ValidateAndHeal(
            host.Services,
            host.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(StartupConfigValidationService)));
        return host;
    }

    public static IHost InitializePianoPipeline(this IHost host)
    {
        // Initialize audio and register it as a note event handler
        IAudioOutput audio = host.Services.GetRequiredService<IAudioOutput>();
        audio.Initialize();

        IPianoEngine engine = host.Services.GetRequiredService<IPianoEngine>();
        engine.RegisterHandler(audio);

        // Register UI as a note event handler (key highlighting)
        PianoViewModel pianoViewModel = host.Services.GetRequiredService<PianoViewModel>();
        engine.RegisterHandler(pianoViewModel);

        return host;
    }

    public static IHost InitializeMidiPlaybackCoordinator(this IHost host)
    {
        // Resolve the coordinator to trigger its constructor, which subscribes to player + engine events.
        host.Services.GetRequiredService<MidiPlaybackCoordinator>();
        return host;
    }

    public static IHost AutoLoadMidi(this IHost host)
    {
        // Auto-load the MIDI file persisted in PlayerConfig.Path, if any.
        string path = host.Services.GetRequiredService<IOptions<PlayerConfig>>().Value.Path;
        if (string.IsNullOrEmpty(path))
        {
            return host;
        }
        host.Services.GetRequiredService<IMidiFileLoader>().TryLoadAsync(path).GetAwaiter().GetResult();
        return host;
    }

    public static IHost ApplyColorTheme(this IHost host)
    {
        // Set active theme and load user colors before any views are created
        UIConfig uiConfig = host.Services.GetRequiredService<IOptions<UIConfig>>().Value;
        ThemePaletteProvider.SetActive(uiConfig.Theme switch
        {
            AppTheme.Light => ThemePaletteProvider.Light,
            _ => ThemePaletteProvider.Dark,
        });
        UserPalette.Initialize(uiConfig.Colors);

        return host;
    }

    public static int RunAvaloniaApp(this IHost host, string[] args)
    {
        try
        {
            AppBuilder.Configure(() => new App(() => host.Services.GetRequiredService<MainWindow>()))
                .UsePlatformDetect()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();

            logger.LogError(e, "Unhandled exception: {Message}", e.Message);

            return 1;
        }
        return 0;
    }
}
