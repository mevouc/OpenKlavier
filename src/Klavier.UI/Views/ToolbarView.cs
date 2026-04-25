using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
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
    private readonly FilePathPicker _midiPicker;

    private bool _isSettingsOpen;

    public event Action<bool>? SettingsToggled;

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

        TextButton settingsButton = new("Settings", momentaryActiveOnPress: false) { Margin = new Thickness(4, 0, 0, 0) };
        settingsButton.PointerPressed += (_, e) =>
        {
            _isSettingsOpen = !_isSettingsOpen;
            settingsButton.IsActive = _isSettingsOpen;
            SettingsToggled?.Invoke(_isSettingsOpen);
            e.Handled = true;
        };

        _midiPicker = new FilePathPicker(
            _MidiPickerTitle,
            _MidiTooltip,
            new FilePickerFileType("MIDI files") { Patterns = ["*.mid", "*.midi"] },
            () => playerConfig.CurrentValue.Path,
            () => _midiPlayer.CurrentScore?.DisplayName,
            HandleMidiPath)
        {
            Margin = new Thickness(4, 0, 0, 0),
        };

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { panicButton, settingsButton, _midiPicker },
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
        return true;
    }
}
