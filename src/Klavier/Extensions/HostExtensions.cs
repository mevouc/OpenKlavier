using Avalonia;
using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.Core.Ports;
using Klavier.Midi;
using Klavier.Midi.Player;
using Klavier.Midi.Ports;
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
        StartupConfigValidator.ValidateAndHeal(
            host.Services,
            host.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(StartupConfigValidator)));
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
        // TEMP smoke-test: autoload westworld.mid from Desktop and start playing.
        // Remove once Step 7 (drag-drop / file picker) lands.
        string testMidiPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "westworld.mid");
        IMidiScoreLoader midiLoader = host.Services.GetRequiredService<IMidiScoreLoader>();
        IMidiPlayer midiPlayer = host.Services.GetRequiredService<IMidiPlayer>();
        MidiScore testScore = midiLoader.LoadAsync(testMidiPath).GetAwaiter().GetResult();
        midiPlayer.Load(testScore);

        try
        {
            AppBuilder.Configure(() => new App(() => host.Services.GetRequiredService<MainWindow>()))
                .UsePlatformDetect()
                .AfterSetup(_ => midiPlayer.Play()) // TEMP smoke-test: defer Play until Avalonia's dispatcher is bound
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogError(e, "Unhandled exception: {Message}", e.Message);

            return 1;
        }
        return 0;
    }
}
