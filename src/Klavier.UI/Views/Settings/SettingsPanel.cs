using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.Config;
using Klavier.Core.Primitives;
using Klavier.SoundFont;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Ports;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Settings;
using Klavier.UI.Views.Settings.KeybindsEditor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public partial class SettingsPanel : Border
{
    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _SubtextBrush = new(ThemePaletteProvider.TextPrimary) { Opacity = 0.7 };
    private static readonly SolidColorBrush _ContrastedSurfaceBrush = new(ThemePaletteProvider.ContrastedSurface);
    private static readonly SolidColorBrush _NeutralSurfaceBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _HoverHighlightBrush = new(ThemePaletteProvider.HoverHighlight);
    private const double _LabelWidth = 130;
    private const double _ValueWidth = 40;
    private const double _MinRowHeight = 32;
    private const double _RowIndent = 20;

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
    private readonly Func<KeyboardMapping, string?, KeybindsEditorWindow> _createKeybindsEditor;
    private readonly ILogger<SettingsPanel> _logger;

    public SettingsPanel(
        IUserSettingsService settingsService,
        ISoundFontInfoProvider soundFontInfoProvider,
        IOptionsMonitor<PianoConfig> pianoConfig,
        IOptionsMonitor<AudioConfig> audioConfig,
        IOptionsMonitor<PlayerConfig> playerConfig,
        IOptionsMonitor<UIConfig> uiConfig,
        Func<KeyboardMapping, string?, KeybindsEditorWindow> createKeybindsEditor,
        ILogger<SettingsPanel> logger)
    {
        _settingsService = settingsService;
        _createKeybindsEditor = createKeybindsEditor;
        _logger = logger;

        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Padding = new Thickness(12, 8);
        IsVisible = false;

        PianoConfig piano = pianoConfig.CurrentValue;
        AudioConfig audio = audioConfig.CurrentValue;
        PlayerConfig player = playerConfig.CurrentValue;
        UIConfig ui = uiConfig.CurrentValue;

        Slider velocitySlider = CreateSlider(NoteVelocity.MinValue, NoteVelocity.MaxValue, piano.Velocity);
        TextBlock velocityValue = CreateValueLabel(piano.Velocity.ToString());

        Slider transposeSlider = CreateSlider(Transpose.MinValue, Transpose.MaxValue, piano.Transpose);
        TextBlock transposeValue = CreateValueLabel(piano.Transpose.ToString());

        Slider volumeSlider = CreateSlider(0, 120, audio.VolumeInPercent);
        TextBlock volumeValue = CreateValueLabel($"{audio.VolumeInPercent}%");

        Slider tempoSlider = CreateSlider(25, 200, (int)Math.Round(player.TempoMultiplier * 100));
        TextBlock tempoValue = CreateValueLabel($"{player.TempoMultiplier:0.00}x");

        Slider lookaheadSlider = CreateSlider(1, 10, player.LookaheadSeconds);
        TextBlock lookaheadValue = CreateValueLabel($"{player.LookaheadSeconds} s");

        ComboBox sustainModeCombo = CreateComboBox(ui.SustainMode);
        sustainModeCombo.ItemTemplate = new FuncDataTemplate<SustainMode>((mode, _) => new TextBlock
        {
            Text = mode switch
            {
                SustainMode.InvertedHold => "Inverted hold",
                _ => mode.ToString(),
            },
        });
        ToggleSwitch topmostToggle = CreateToggleSwitch(ui.Topmost);
        ToggleSwitch keyLabelsToggle = CreateToggleSwitch(ui.ShowKeyLabels);
        ToggleSwitch noteLabelsToggle = CreateToggleSwitch(ui.ShowNoteLabels);
        ComboBox noteNameStyleCombo = CreateComboBox(ui.NoteNameStyle);
        ComboBox themeCombo = CreateComboBox(ui.Theme);
        ComboBox keyboardLayoutCombo = CreateComboBox(
            KeyboardMappingProvider.GetAvailableLayouts(),
            ui.KeyboardLayout);
        IconButton createLayoutButton = CreatePlusIconButton();
        IconButton editLayoutButton = CreatePencilIconButton();
        StackPanel keyboardLayoutRow = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { keyboardLayoutCombo, createLayoutButton, editLayoutButton },
        };
        WireKeybindsEditorButton(createLayoutButton, uiConfig, useCurrentLayoutName: false);
        WireKeybindsEditorButton(editLayoutButton, uiConfig, useCurrentLayoutName: true);

        SoundFontInfo soundFontInfo = soundFontInfoProvider.GetSoundFontInfo();
        ComboBox presetCombo = CreateComboBox(soundFontInfo.Presets.Values, FindPreset(soundFontInfo.Presets, audio.SoundFont.Preset));

        FilePathPicker soundFontPicker = new(
            _SoundFontPickerTitle,
            _SoundFontTooltip,
            new FilePickerFileType("SoundFont") { Patterns = ["*.sf2", "*.sf3"] },
            () => audioConfig.CurrentValue.SoundFont.Path,
            () => soundFontInfoProvider.GetSoundFontInfo().Name,
            newPath => HandleSoundFontPath(newPath, audioConfig));

        TextBox accentHexTextBox = CreateHexColorTextBox(UserPalette.Accent);
        TextBox whiteKeyHexTextBox = CreateHexColorTextBox(UserPalette.WhiteKey);
        TextBox blackKeyHexTextBox = CreateHexColorTextBox(UserPalette.BlackKey);
        TextBox keyBorderHexTextBox = CreateHexColorTextBox(UserPalette.KeyBorder);

        // Wire sliders
        WireSlider(velocitySlider, velocityValue, ConfigKey.Of(PianoConfig.SectionName, nameof(PianoConfig.Velocity)));
        WireSlider(transposeSlider, transposeValue, ConfigKey.Of(PianoConfig.SectionName, nameof(PianoConfig.Transpose)));
        WireSlider(volumeSlider, volumeValue, ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.VolumeInPercent)), val => $"{val}%");
        WireSlider(lookaheadSlider, lookaheadValue, ConfigKey.Of(PlayerConfig.SectionName, nameof(PlayerConfig.LookaheadSeconds)), val => $"{val} s");
        // Tempo: slider is 25-200 (percent), config stores 0.25-2.0 (multiplier).
        tempoSlider.ValueChanged += (_, e) =>
        {
            int percent = (int)e.NewValue;
            double tempo = percent / 100.0;
            tempoValue.Text = $"{tempo:0.00}x";
            _settingsService.UpdateSetting(
                ConfigKey.Of(PlayerConfig.SectionName, nameof(PlayerConfig.TempoMultiplier)),
                tempo);
        };

        // Wire dropdowns
        WireComboBox(sustainModeCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.SustainMode)));
        WireComboBox(noteNameStyleCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.NoteNameStyle)));
        WireComboBox(themeCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Theme)));
        WireComboBox(keyboardLayoutCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.KeyboardLayout)));
        WirePresetComboBox(presetCombo, ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont), nameof(SoundFontConfig.Preset)));

        // Wire toggles
        WireToggle(topmostToggle, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Topmost)));
        WireToggle(keyLabelsToggle, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.ShowKeyLabels)));
        WireToggle(noteLabelsToggle, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.ShowNoteLabels)));

        // Wire color hex textboxes
        WireHexColorTextBox(accentHexTextBox, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.Accent)));
        WireHexColorTextBox(whiteKeyHexTextBox, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.WhiteKey)));
        WireHexColorTextBox(blackKeyHexTextBox, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.BlackKey)));
        WireHexColorTextBox(keyBorderHexTextBox, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.KeyBorder)));

        // Sync controls when config reloads (covers reset + external changes)
        pianoConfig.OnChange(newPiano => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            velocitySlider.Value = newPiano.Velocity;
            transposeSlider.Value = newPiano.Transpose;
        }));

        audioConfig.OnChange(newAudio => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            volumeSlider.Value = newAudio.VolumeInPercent;
            SoundFontPreset? preset = FindPreset(soundFontInfoProvider.GetSoundFontInfo().Presets, newAudio.SoundFont.Preset);
            if (preset.HasValue)
            {
                presetCombo.SelectedItem = preset.Value;
            }
        }));

        playerConfig.OnChange(newPlayer => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            tempoSlider.Value = (int)Math.Round(newPlayer.TempoMultiplier * 100);
            lookaheadSlider.Value = newPlayer.LookaheadSeconds;
        }));

        soundFontInfoProvider.SoundFontInfoChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            SoundFontInfo updatedInfo = soundFontInfoProvider.GetSoundFontInfo();
            presetCombo.ItemsSource = updatedInfo.Presets.Values;
            SoundFontPreset? preset = FindPreset(updatedInfo.Presets, audioConfig.CurrentValue.SoundFont.Preset);
            if (preset.HasValue)
            {
                presetCombo.SelectedItem = preset.Value;
            }
            soundFontPicker.Refresh();
        });

        uiConfig.OnChange(newUi => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            sustainModeCombo.SelectedItem = newUi.SustainMode;
            topmostToggle.IsChecked = newUi.Topmost;
            keyLabelsToggle.IsChecked = newUi.ShowKeyLabels;
            noteLabelsToggle.IsChecked = newUi.ShowNoteLabels;
            noteNameStyleCombo.SelectedItem = newUi.NoteNameStyle;
            themeCombo.SelectedItem = newUi.Theme;
            keyboardLayoutCombo.SelectedItem = newUi.KeyboardLayout;
        }));

        KeyboardMappingProvider.LayoutsChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            string? currentSelection = keyboardLayoutCombo.SelectedItem as string;
            keyboardLayoutCombo.ItemsSource = KeyboardMappingProvider.GetAvailableLayouts();
            keyboardLayoutCombo.SelectedItem = currentSelection;
        });

        // Wire reset
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
                    CreateRow(_VelocityLabel, velocityValue, velocitySlider, tooltip: _VelocityTooltip),
                    CreateRow(_TransposeLabel, transposeValue, transposeSlider, tooltip: _TransposeTooltip),
                    CreateRow(_VolumeLabel, volumeValue, volumeSlider),
                    CreateRow(_TempoLabel, tempoValue, tempoSlider, tooltip: _TempoTooltip),
                    CreateRow(_LookaheadLabel, lookaheadValue, lookaheadSlider, tooltip: _LookaheadTooltip),
                    CreateRow(_SustainModeLabel, sustainModeCombo, tooltip: _SustainModeTooltip),
                    CreateRow(_SoundFontLabel, soundFontPicker, tooltip: _SoundFontTooltip),
                    CreateRow(_PresetLabel, presetCombo, tooltip: _PresetTooltip),

                    CreateSectionHeader(_PianoDisplaySectionTitle),
                    CreateRow(_ShowKeyLabelsLabel, keyLabelsToggle, tooltip: _ShowKeyLabelsTooltip),
                    CreateRow(_ShowNoteLabelsLabel, noteLabelsToggle, tooltip: _ShowNoteLabelsTooltip),
                    CreateRow(_NoteNameStyleLabel, noteNameStyleCombo, tooltip: _NoteNameStyleTooltip),

                    CreateSectionHeader(_ThemeSectionTitle, "(requires restart)"),
                    CreateRow(_ThemeLabel, themeCombo),
                    CreateRow(_AccentLabel, accentHexTextBox, tooltip: _AccentTooltip),
                    CreateRow(_WhiteKeyLabel, whiteKeyHexTextBox),
                    CreateRow(_BlackKeyLabel, blackKeyHexTextBox),
                    CreateRow(_KeyBorderLabel, keyBorderHexTextBox, tooltip: _KeyBorderTooltip),

                    CreateSectionHeader(_WindowSectionTitle),
                    CreateRow(_TopmostLabel, topmostToggle),

                    CreateSectionHeader(_KeyboardSectionTitle),
                    CreateRow(_KeyboardLayoutLabel, keyboardLayoutRow, tooltip: _KeyboardLayoutTooltip),

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
}
