using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Primitives;
using Klavier.SoundFont;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Ports;
using Klavier.UI.Theme;
using Klavier.UI.Views.Settings;
using Klavier.UI.Views.Toolbar;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public partial class SettingsPanel : Border
{
    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _ContrastedSurfaceBrush = new(ThemePaletteProvider.ContrastedSurface);
    private static readonly SolidColorBrush _NeutralSurfaceBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _HoverHighlightBrush = new(ThemePaletteProvider.HoverHighlight);
    private const double _LabelWidth = 130;
    private const double _ValueWidth = 40;
    private const double _MinRowHeight = 32;

    private const string _VelocityLabel = "Velocity";
    private const string _TransposeLabel = "Transpose";
    private const string _VolumeLabel = "Volume";
    private const string _SustainModeLabel = "Sustain Mode";
    private const string _TopmostLabel = "Topmost";
    private const string _ShowKeyLabelsLabel = "Show Key Labels";
    private const string _ShowNoteLabelsLabel = "Show Note Labels";
    private const string _NoteNameStyleLabel = "Note Name Style";
    private const string _PresetLabel = "Preset";
    private const string _SoundFontLabel = "SoundFont";
    private const string _ThemeLabel = "Theme (restart)";
    private const string _AccentLabel = "Accent (restart)";
    private const string _WhiteKeyLabel = "White Key (restart)";
    private const string _BlackKeyLabel = "Black Key (restart)";
    private const string _KeyBorderLabel = "Key Border (restart)";
    private const string _KeyboardLayoutLabel = "Keyboard Layout";
    private const string _ResetDefaultsButtonLabel = "Reset Defaults";

    private readonly IUserSettingsService _settingsService;

    public SettingsPanel(
        IUserSettingsService settingsService,
        ISoundFontInfoProvider soundFontInfoProvider,
        IOptionsMonitor<PianoConfig> pianoConfig,
        IOptionsMonitor<AudioConfig> audioConfig,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _settingsService = settingsService;

        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Padding = new Thickness(12, 8);
        IsVisible = false;

        PianoConfig piano = pianoConfig.CurrentValue;
        AudioConfig audio = audioConfig.CurrentValue;
        UIConfig ui = uiConfig.CurrentValue;

        Slider velocitySlider = CreateSlider(NoteVelocity.MinValue, NoteVelocity.MaxValue, piano.Velocity);
        TextBlock velocityValue = CreateValueLabel(piano.Velocity.ToString());

        Slider transposeSlider = CreateSlider(Transpose.MinValue, Transpose.MaxValue, piano.Transpose);
        TextBlock transposeValue = CreateValueLabel(piano.Transpose.ToString());

        Slider volumeSlider = CreateSlider(0, 120, audio.VolumeInPercent);
        TextBlock volumeValue = CreateValueLabel($"{audio.VolumeInPercent}%");

        ComboBox sustainModeCombo = CreateComboBox(ui.SustainMode);
        ToggleSwitch topmostToggle = CreateToggleSwitch(ui.Topmost);
        ToggleSwitch keyLabelsToggle = CreateToggleSwitch(ui.ShowKeyLabels);
        ToggleSwitch noteLabelsToggle = CreateToggleSwitch(ui.ShowNoteLabels);
        ComboBox noteNameStyleCombo = CreateComboBox(ui.NoteNameStyle);
        ComboBox themeCombo = CreateComboBox(ui.Theme);
        ComboBox keyboardLayoutCombo = CreateComboBox(
            KeyboardMappingProvider.GetAvailableLayouts(),
            ui.KeyboardLayout);

        SoundFontInfo soundFontInfo = soundFontInfoProvider.GetSoundFontInfo();
        ComboBox presetCombo = CreateComboBox(soundFontInfo.Presets.Values, FindPreset(soundFontInfo.Presets, audio.SoundFont.Preset));

        (string soundFontDisplay, string? soundFontTooltip) = GetSoundFontDisplayName(soundFontInfo.Name, audio.SoundFont.Path);
        TextBox soundFontPathDisplay = CreateSoundFontPathDisplay(soundFontDisplay, soundFontTooltip);
        PathIconButton soundFontPickerButton = CreateSoundFontPickerButton();
        Border soundFontPickerControl = CreateSoundFontPickerControl(soundFontPathDisplay, soundFontPickerButton);

        TextBox accentHexTextBox = CreateHexColorTextBox(UserPalette.Accent);
        TextBox whiteKeyHexTextBox = CreateHexColorTextBox(UserPalette.WhiteKey);
        TextBox blackKeyHexTextBox = CreateHexColorTextBox(UserPalette.BlackKey);
        TextBox keyBorderHexTextBox = CreateHexColorTextBox(UserPalette.KeyBorder);

        // Wire sliders
        WireSlider(velocitySlider, velocityValue, ConfigKey.Of(PianoConfig.SectionName, nameof(PianoConfig.Velocity)));
        WireSlider(transposeSlider, transposeValue, ConfigKey.Of(PianoConfig.SectionName, nameof(PianoConfig.Transpose)));
        WireSlider(volumeSlider, volumeValue, ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.VolumeInPercent)), val => $"{val}%");

        // Wire dropdowns
        WireComboBox(sustainModeCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.SustainMode)));
        WireComboBox(noteNameStyleCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.NoteNameStyle)));
        WireComboBox(themeCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Theme)));
        WireComboBox(keyboardLayoutCombo, ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.KeyboardLayout)));
        WirePresetComboBox(presetCombo, ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont), nameof(SoundFontConfig.Preset)));
        WireSoundFontPicker(soundFontPickerButton, audioConfig);

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

        soundFontInfoProvider.SoundFontInfoChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            SoundFontInfo updatedInfo = soundFontInfoProvider.GetSoundFontInfo();
            presetCombo.ItemsSource = updatedInfo.Presets.Values;
            SoundFontPreset? preset = FindPreset(updatedInfo.Presets, audioConfig.CurrentValue.SoundFont.Preset);
            if (preset.HasValue)
            {
                presetCombo.SelectedItem = preset.Value;
            }
            (string display, string? tooltip) = GetSoundFontDisplayName(updatedInfo.Name, audioConfig.CurrentValue.SoundFont.Path);
            soundFontPathDisplay.Text = display;
            ToolTip.SetTip(soundFontPathDisplay, tooltip);
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

        // Wire reset
        ToolbarButton resetButton = new(_ResetDefaultsButtonLabel);
        resetButton.PointerPressed += (_, e) =>
        {
            _settingsService.ResetAll();
            resetButton.IsActive = true;
            e.Handled = true;
        };
        resetButton.PointerReleased += (_, e) =>
        {
            resetButton.IsActive = false;
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
                    CreateRow(_VelocityLabel, velocityValue, velocitySlider),
                    CreateRow(_TransposeLabel, transposeValue, transposeSlider),
                    CreateRow(_VolumeLabel, volumeValue, volumeSlider),
                    CreateRow(_SoundFontLabel, soundFontPickerControl),
                    CreateRow(_PresetLabel, presetCombo),
                    CreateRow(_SustainModeLabel, sustainModeCombo),
                    CreateRow(_TopmostLabel, topmostToggle),
                    CreateRow(_ShowKeyLabelsLabel, keyLabelsToggle),
                    CreateRow(_ShowNoteLabelsLabel, noteLabelsToggle),
                    CreateRow(_NoteNameStyleLabel, noteNameStyleCombo),
                    CreateRow(_ThemeLabel, themeCombo),
                    CreateRow(_AccentLabel, accentHexTextBox),
                    CreateRow(_WhiteKeyLabel, whiteKeyHexTextBox),
                    CreateRow(_BlackKeyLabel, blackKeyHexTextBox),
                    CreateRow(_KeyBorderLabel, keyBorderHexTextBox),
                    CreateRow(_KeyboardLayoutLabel, keyboardLayoutCombo),
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
