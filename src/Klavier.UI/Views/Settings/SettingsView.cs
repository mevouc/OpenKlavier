using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.Config.Schema;
using Klavier.Config.UserSettings;
using Klavier.Core.Primitives;
using Klavier.SoundFont;
using Klavier.SoundFont.Loading;
using Klavier.SoundFont.Ports;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;
using Klavier.UI.Threading;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Settings.KeybindsEditor;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Settings;

public partial class SettingsView : Border
{
    private const string _VelocityLabel = "Velocity";
    private const string _TransposeLabel = "Transpose";
    private const string _VolumeLabel = "Volume";
    private const string _TempoLabel = "Playback speed";
    private const string _LookaheadLabel = "Lookahead";
    private const string _SustainModeLabel = "Sustain behavior";
    private const string _TopmostLabel = "Always on top";
    private const string _ShowKeyLabelsLabel = "Show keyboard keys";
    private const string _ShowNoteLabelsLabel = "Show note names";
    private const string _NoteNameStyleLabel = "Note notation";
    private const string _PresetLabel = "Instrument";
    private const string _SoundFontLabel = "SoundFont";
    private const string _ThemeLabel = "Theme";
    private const string _AccentLabel = "Accent color";
    private const string _WhiteKeyLabel = "White key color";
    private const string _BlackKeyLabel = "Black key color";
    private const string _KeyBorderLabel = "Key border color";
    private const string _KeyboardLayoutLabel = "Keyboard layout";
    private const string _ResetDefaultsButtonLabel = "Reset defaults";

    private const string _VelocityTooltip = "How hard keys are pressed (0 - 127).\nHigher: louder and brighter timbre";
    private const string _TransposeTooltip = "Shift note pitches up or down by semitones (-24 - 24)";
    private const string _TempoTooltip = "MIDI file playback speed (0.25x = quarter speed, 2.0x = double speed)";
    private const string _LookaheadTooltip = "How many seconds of upcoming notes are shown above the piano";
    private const string _SustainModeTooltip = "Hold: sustain while pressed\nInverted hold: sustain while released\nToggle: press to flip on/off";
    private const string _ShowKeyLabelsTooltip = "Overlay computer keyboard letters on piano keys";
    private const string _ShowNoteLabelsTooltip = "Overlay musical note names on piano keys";
    private const string _NoteNameStyleTooltip = "Scientific: C4\nSolfege: Do\nHelmholtz: c'";
    private const string _SoundFontTooltip = "A .sf2/.sf3 file defining instrument sounds";
    private const string _PresetTooltip = "Instrument sound from the SoundFont file";
    private const string _AccentTooltip = "Highlight color used across the UI";
    private const string _KeyBorderTooltip = "Color of the outline around each key";
    private const string _KeyboardLayoutTooltip = "Mapping of computer keys to piano notes";

    private const string _SoundSectionTitle = "Sound & Playback";
    private const string _PianoDisplaySectionTitle = "Piano Display";
    private const string _KeyboardSectionTitle = "Keyboard";
    private const string _WindowSectionTitle = "Window";
    private const string _ThemeSectionTitle = "Theme & Colors";

    private readonly IUserSettingsService _settingsService;
    private readonly ISoundFontInfoProvider _soundFontInfoProvider;
    private readonly ISoundFontFileLoader _soundFontFileLoader;
    private readonly IOptionsMonitor<PianoConfig> _pianoConfig;
    private readonly IOptionsMonitor<AudioConfig> _audioConfig;
    private readonly IOptionsMonitor<PlayerConfig> _playerConfig;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly IKeyboardMappingService _keyboardMappingService;
    private readonly Func<KeyboardMapping, string?, KeybindsEditorWindow> _createKeybindsEditor;

    public SettingsView(
        IUserSettingsService settingsService,
        ISoundFontInfoProvider soundFontInfoProvider,
        ISoundFontFileLoader soundFontFileLoader,
        IOptionsMonitor<PianoConfig> pianoConfig,
        IOptionsMonitor<AudioConfig> audioConfig,
        IOptionsMonitor<PlayerConfig> playerConfig,
        IOptionsMonitor<UIConfig> uiConfig,
        IKeyboardMappingService keyboardMappingService,
        Func<KeyboardMapping, string?, KeybindsEditorWindow> createKeybindsEditor)
    {
        _settingsService = settingsService;
        _soundFontInfoProvider = soundFontInfoProvider;
        _soundFontFileLoader = soundFontFileLoader;
        _pianoConfig = pianoConfig;
        _audioConfig = audioConfig;
        _playerConfig = playerConfig;
        _uiConfig = uiConfig;
        _keyboardMappingService = keyboardMappingService;
        _createKeybindsEditor = createKeybindsEditor;

        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Padding = new Thickness(12, 8);
        IsVisible = false;

        TextButton resetButton = new(_ResetDefaultsButtonLabel);
        resetButton.PointerPressed += (_, e) =>
        {
            _settingsService.ResetAll();
            e.Handled = true;
        };

        ScrollViewer scrollViewer = new()
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
                Margin = new Thickness(0, 0, 16, 0),
                Children =
                {
                    CreateSectionHeader(_SoundSectionTitle),
                    BuildVelocityRow(),
                    BuildTransposeRow(),
                    BuildVolumeRow(),
                    BuildTempoRow(),
                    BuildLookaheadRow(),
                    BuildSustainModeRow(),
                    BuildSoundFontRow(),
                    BuildPresetRow(),

                    CreateSectionHeader(_PianoDisplaySectionTitle),
                    BuildShowKeyLabelsRow(),
                    BuildShowNoteLabelsRow(),
                    BuildNoteNameStyleRow(),

                    CreateSectionHeader(_ThemeSectionTitle, "(requires restart)"),
                    BuildThemeRow(),
                    BuildAccentColorRow(),
                    BuildWhiteKeyColorRow(),
                    BuildBlackKeyColorRow(),
                    BuildKeyBorderColorRow(),

                    CreateSectionHeader(_WindowSectionTitle),
                    BuildTopmostRow(),

                    CreateSectionHeader(_KeyboardSectionTitle),
                    BuildKeyboardLayoutRow(),

                    CreateResetRow(resetButton),
                },
            },
        };
        scrollViewer.AddHandler(
            RequestBringIntoViewEvent,
            (_, e) => e.Handled = true,
            handledEventsToo: true);
        Child = scrollViewer;
    }

    private DockPanel BuildVelocityRow()
    {
        int initialValue = _pianoConfig.CurrentValue.Velocity;
        Slider slider = CreateSlider(NoteVelocity.MinValue, NoteVelocity.MaxValue, initialValue);
        TextBlock value = CreateValueLabel(initialValue.ToString());
        WireSlider(slider, value, PianoConfig.Keys.Velocity);
        _pianoConfig.OnChangeOnUIThread(c => slider.Value = c.Velocity);
        return CreateRow(_VelocityLabel, value, slider, tooltip: _VelocityTooltip);
    }

    private DockPanel BuildTransposeRow()
    {
        int initialValue = _pianoConfig.CurrentValue.Transpose;
        Slider slider = CreateSlider(Transpose.MinValue, Transpose.MaxValue, initialValue);
        TextBlock value = CreateValueLabel(initialValue.ToString());
        WireSlider(slider, value, PianoConfig.Keys.Transpose);
        _pianoConfig.OnChangeOnUIThread(c => slider.Value = c.Transpose);
        return CreateRow(_TransposeLabel, value, slider, tooltip: _TransposeTooltip);
    }

    private DockPanel BuildVolumeRow()
    {
        ushort initialValue = _audioConfig.CurrentValue.VolumeInPercent;
        Slider slider = CreateSlider(0, 120, initialValue);
        TextBlock value = CreateValueLabel($"{initialValue}%");
        WireSlider(slider, value, AudioConfig.Keys.VolumeInPercent, val => $"{val}%");
        _audioConfig.OnChangeOnUIThread(c => slider.Value = c.VolumeInPercent);
        return CreateRow(_VolumeLabel, value, slider);
    }

    private DockPanel BuildTempoRow()
    {
        // Slider is 25-200 (percent), config stores 0.25-2.0 (multiplier).
        double initialMultiplier = _playerConfig.CurrentValue.TempoMultiplier;
        Slider slider = CreateSlider(25, 200, (int)Math.Round(initialMultiplier * 100));
        TextBlock value = CreateValueLabel($"{initialMultiplier:0.00}x");
        slider.ValueChanged += (_, e) =>
        {
            int percent = (int)e.NewValue;
            double tempo = percent / 100.0;
            value.Text = $"{tempo:0.00}x";
            _settingsService.UpdateSetting(PlayerConfig.Keys.TempoMultiplier, tempo);
        };
        _playerConfig.OnChangeOnUIThread(c => slider.Value = (int)Math.Round(c.TempoMultiplier * 100));
        return CreateRow(_TempoLabel, value, slider, tooltip: _TempoTooltip);
    }

    private DockPanel BuildLookaheadRow()
    {
        int initialValue = _playerConfig.CurrentValue.LookaheadSeconds;
        Slider slider = CreateSlider(1, 10, initialValue);
        TextBlock value = CreateValueLabel($"{initialValue} s");
        WireSlider(slider, value, PlayerConfig.Keys.LookaheadSeconds, val => $"{val} s");
        _playerConfig.OnChangeOnUIThread(c => slider.Value = c.LookaheadSeconds);
        return CreateRow(_LookaheadLabel, value, slider, tooltip: _LookaheadTooltip);
    }

    private DockPanel BuildSustainModeRow()
    {
        ComboBox combo = CreateComboBox(_uiConfig.CurrentValue.SustainMode);
        combo.ItemTemplate = new FuncDataTemplate<SustainMode>((mode, _) => new TextBlock
        {
            Text = mode switch
            {
                SustainMode.InvertedHold => "Inverted hold",
                _ => mode.ToString(),
            },
        });
        WireComboBox(combo, UIConfig.Keys.SustainMode);
        _uiConfig.OnChangeOnUIThread(c => combo.SelectedItem = c.SustainMode);
        return CreateRow(_SustainModeLabel, combo, tooltip: _SustainModeTooltip);
    }

    private DockPanel BuildSoundFontRow()
    {
        FilePathPicker picker = new(
            _SoundFontPickerTitle,
            _SoundFontTooltip,
            new FilePickerFileType("SoundFont") { Patterns = ["*.sf2", "*.sf3"] },
            () => _audioConfig.CurrentValue.SoundFont.Path,
            () => _soundFontInfoProvider.GetSoundFontInfo().Name,
            _soundFontFileLoader.TryLoadAsync);
        _soundFontInfoProvider.SoundFontInfoChanged += UIThread.Post(picker.Refresh);
        return CreateRow(_SoundFontLabel, picker, tooltip: _SoundFontTooltip);
    }

    private DockPanel BuildPresetRow()
    {
        SoundFontInfo info = _soundFontInfoProvider.GetSoundFontInfo();
        ComboBox combo = CreateComboBox(info.Presets.Values, FindPreset(info.Presets, _audioConfig.CurrentValue.SoundFont.Preset));
        WirePresetComboBox(combo, AudioConfig.Keys.SoundFont.Preset);
        _audioConfig.OnChangeOnUIThread(c =>
        {
            SoundFontPreset? preset = FindPreset(_soundFontInfoProvider.GetSoundFontInfo().Presets, c.SoundFont.Preset);
            if (preset.HasValue)
            {
                combo.SelectedItem = preset.Value;
            }
        });
        _soundFontInfoProvider.SoundFontInfoChanged += UIThread.Post(() =>
        {
            SoundFontInfo updatedInfo = _soundFontInfoProvider.GetSoundFontInfo();
            combo.ItemsSource = updatedInfo.Presets.Values;
            SoundFontPreset? preset = FindPreset(updatedInfo.Presets, _audioConfig.CurrentValue.SoundFont.Preset);
            if (preset.HasValue)
            {
                combo.SelectedItem = preset.Value;
            }
        });
        return CreateRow(_PresetLabel, combo, tooltip: _PresetTooltip);
    }

    private DockPanel BuildShowKeyLabelsRow()
    {
        ToggleSwitch toggle = CreateToggleSwitch(_uiConfig.CurrentValue.ShowKeyLabels);
        WireToggle(toggle, UIConfig.Keys.ShowKeyLabels);
        _uiConfig.OnChangeOnUIThread(c => toggle.IsChecked = c.ShowKeyLabels);
        return CreateRow(_ShowKeyLabelsLabel, toggle, tooltip: _ShowKeyLabelsTooltip);
    }

    private DockPanel BuildShowNoteLabelsRow()
    {
        ToggleSwitch toggle = CreateToggleSwitch(_uiConfig.CurrentValue.ShowNoteLabels);
        WireToggle(toggle, UIConfig.Keys.ShowNoteLabels);
        _uiConfig.OnChangeOnUIThread(c => toggle.IsChecked = c.ShowNoteLabels);
        return CreateRow(_ShowNoteLabelsLabel, toggle, tooltip: _ShowNoteLabelsTooltip);
    }

    private DockPanel BuildNoteNameStyleRow()
    {
        ComboBox combo = CreateComboBox(_uiConfig.CurrentValue.NoteNameStyle);
        WireComboBox(combo, UIConfig.Keys.NoteNameStyle);
        _uiConfig.OnChangeOnUIThread(c => combo.SelectedItem = c.NoteNameStyle);
        return CreateRow(_NoteNameStyleLabel, combo, tooltip: _NoteNameStyleTooltip);
    }

    private DockPanel BuildThemeRow()
    {
        ComboBox combo = CreateComboBox(_uiConfig.CurrentValue.Theme);
        WireComboBox(combo, UIConfig.Keys.Theme);
        _uiConfig.OnChangeOnUIThread(c => combo.SelectedItem = c.Theme);
        return CreateRow(_ThemeLabel, combo);
    }

    private DockPanel BuildAccentColorRow()
    {
        TextBox textBox = CreateHexColorTextBox(UserPalette.Accent);
        WireHexColorTextBox(textBox, UIConfig.Keys.Colors.Accent);
        return CreateRow(_AccentLabel, textBox, tooltip: _AccentTooltip);
    }

    private DockPanel BuildWhiteKeyColorRow()
    {
        TextBox textBox = CreateHexColorTextBox(UserPalette.WhiteKey);
        WireHexColorTextBox(textBox, UIConfig.Keys.Colors.WhiteKey);
        return CreateRow(_WhiteKeyLabel, textBox);
    }

    private DockPanel BuildBlackKeyColorRow()
    {
        TextBox textBox = CreateHexColorTextBox(UserPalette.BlackKey);
        WireHexColorTextBox(textBox, UIConfig.Keys.Colors.BlackKey);
        return CreateRow(_BlackKeyLabel, textBox);
    }

    private DockPanel BuildKeyBorderColorRow()
    {
        TextBox textBox = CreateHexColorTextBox(UserPalette.KeyBorder);
        WireHexColorTextBox(textBox, UIConfig.Keys.Colors.KeyBorder);
        return CreateRow(_KeyBorderLabel, textBox, tooltip: _KeyBorderTooltip);
    }

    private DockPanel BuildTopmostRow()
    {
        ToggleSwitch toggle = CreateToggleSwitch(_uiConfig.CurrentValue.Topmost);
        WireToggle(toggle, UIConfig.Keys.Topmost);
        _uiConfig.OnChangeOnUIThread(c => toggle.IsChecked = c.Topmost);
        return CreateRow(_TopmostLabel, toggle);
    }

    private DockPanel BuildKeyboardLayoutRow()
    {
        ComboBox combo = CreateComboBox(_keyboardMappingService.GetAvailableLayouts(), _uiConfig.CurrentValue.KeyboardLayout);
        IconButton createLayoutButton = CreatePlusIconButton();
        IconButton editLayoutButton = CreatePencilIconButton();
        StackPanel layoutRow = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { combo, createLayoutButton, editLayoutButton },
        };
        WireKeybindsEditorButton(createLayoutButton, useCurrentLayoutName: false);
        WireKeybindsEditorButton(editLayoutButton, useCurrentLayoutName: true);
        WireComboBox(combo, UIConfig.Keys.KeyboardLayout);
        _uiConfig.OnChangeOnUIThread(c => combo.SelectedItem = c.KeyboardLayout);
        _keyboardMappingService.LayoutsChanged += UIThread.Post(() =>
        {
            string? currentSelection = combo.SelectedItem as string;
            combo.ItemsSource = _keyboardMappingService.GetAvailableLayouts();
            combo.SelectedItem = currentSelection;
        });
        return CreateRow(_KeyboardLayoutLabel, layoutRow, tooltip: _KeyboardLayoutTooltip);
    }
}
