using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.Midi;
using Klavier.Midi.Player;
using Klavier.Midi.Ports;
using Klavier.UI.Ports;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class ToolbarView : Border
{
    private const string _MidiPickerTitle = "Choose a MIDI file";
    private const string _MidiTooltip = "A .mid/.midi file containing piano notes";

    private readonly IMidiScoreLoader _midiLoader;
    private readonly IMidiPlayer _midiPlayer;
    private readonly IUserSettingsService _settingsService;
    private readonly ILogger<ToolbarView> _logger;
    private readonly ToggleTextButton _settingsButton;
    private readonly ToggleTextButton _playerToggleButton;

    public event Action<bool>? SettingsToggled
    {
        add => _settingsButton.Toggled += value;
        remove => _settingsButton.Toggled -= value;
    }

    public event Action<bool>? PlayerToggled
    {
        add => _playerToggleButton.Toggled += value;
        remove => _playerToggleButton.Toggled -= value;
    }

    public ToolbarView(
        IPianoEngine pianoEngine,
        IMidiScoreLoader midiLoader,
        IMidiPlayer midiPlayer,
        IUserSettingsService settingsService,
        IOptionsMonitor<UIConfig> uiConfig,
        IOptionsMonitor<PlayerConfig> playerConfig,
        ILogger<ToolbarView> logger)
    {
        _midiLoader = midiLoader;
        _midiPlayer = midiPlayer;
        _settingsService = settingsService;
        _logger = logger;

        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Padding = new Thickness(8, 4);

        TextButton panicButton = new("Panic");
        panicButton.PointerPressed += (_, e) =>
        {
            pianoEngine.Panic();
            if (uiConfig.CurrentValue.SustainMode == SustainMode.InvertedHold)
            {
                pianoEngine.SustainOn();
            }
            e.Handled = true;
        };

        _settingsButton = new ToggleTextButton("Settings");

        FilePathPicker midiPicker = new(
            _MidiPickerTitle,
            _MidiTooltip,
            new FilePickerFileType("MIDI files") { Patterns = ["*.mid", "*.midi"] },
            () => playerConfig.CurrentValue.Path,
            () => _midiPlayer.CurrentScore?.DisplayName,
            HandleMidiPath);

        _playerToggleButton = new ToggleTextButton("Player") { IsEnabled = _midiPlayer.HasLoadedScore };
        // Auto-load (e.g. AutoLoadMidi at startup) only enables the button; the player is opened only via
        // an explicit click or via HandleMidiPath (user-triggered load).
        _midiPlayer.Loaded += _ => Dispatcher.UIThread.Post(() => _playerToggleButton.IsEnabled = true);

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { panicButton, _settingsButton, midiPicker, _playerToggleButton },
        };
    }

    private async Task<bool> HandleMidiPath(string newPath)
    {
        MidiScore score;
        try
        {
            score = await _midiLoader.LoadAsync(newPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load MIDI file {Path}", newPath);
            return false;
        }

        _midiPlayer.Load(score);
        _settingsService.UpdateSetting(
            ConfigKey.Of(PlayerConfig.SectionName, nameof(PlayerConfig.Path)),
            newPath);

        // User-triggered load: open the player (auto-load via Loaded event does not).
        _playerToggleButton.IsToggled = true;

        return true;
    }
}
