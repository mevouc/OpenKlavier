using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Klavier.Config.Schema;
using Klavier.Core.Engine;
using Klavier.Midi.Loading;
using Klavier.Midi.Playback;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class ToolbarView : Border
{
    private const string _MidiPickerTitle = "Choose a MIDI file";
    private const string _MidiTooltip = "A .mid/.midi file containing piano notes";

    private readonly ToggleTextButton _settingsButton;
    private readonly ToggleTextButton _playerToggleButton;
    private readonly FilePathPicker _midiPicker;

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
        IMidiPlayer midiPlayer,
        IMidiFileLoader midiFileLoader,
        IOptionsMonitor<UIConfig> uiConfig,
        IOptionsMonitor<PlayerConfig> playerConfig)
    {
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

        _midiPicker = new FilePathPicker(
            _MidiPickerTitle,
            _MidiTooltip,
            new FilePickerFileType("MIDI files") { Patterns = ["*.mid", "*.midi"] },
            () => playerConfig.CurrentValue.Path,
            () => midiPlayer.CurrentScore?.DisplayName,
            midiFileLoader.TryLoadAsync);

        _playerToggleButton = new ToggleTextButton("Player") { IsEnabled = midiPlayer.HasLoadedScore };
        // Loaded fires for any load path (file picker, drag-drop, AutoLoadMidi). Enable the button
        // and refresh the picker so external load paths (e.g. drag-drop) update the displayed filename.
        midiPlayer.Loaded += _ => Dispatcher.UIThread.Post(() =>
        {
            _playerToggleButton.IsEnabled = true;
            _midiPicker.Refresh();
        });

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { panicButton, _settingsButton, _midiPicker, _playerToggleButton },
        };
    }
}
