using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Ports;
using Klavier.UI.Theme;
using Klavier.UI.Views.Toolbar;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public partial class SettingsPanel : Border
{
    private static readonly SolidColorBrush _TextBrush = new(KlavierTheme.TextPrimary);
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
    private const string _KeyboardLayoutLabel = "Keyboard Layout";
    private const string _ResetDefaultsButtonLabel = "Reset Defaults";

    private readonly IUserSettingsService _settingsService;

    public SettingsPanel(
        IUserSettingsService settingsService,
        IOptionsMonitor<PianoConfig> pianoConfig,
        IOptionsMonitor<AudioConfig> audioConfig,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _settingsService = settingsService;

        Background = new SolidColorBrush(KlavierTheme.AppBackground);
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
        ComboBox keyboardLayoutCombo = CreateComboBox(
            KeyboardMappingProvider.GetAvailableLayouts(),
            ui.KeyboardLayout);

        // Wire sliders
        WireSlider(velocitySlider, velocityValue, PianoConfig.SectionName, nameof(PianoConfig.Velocity));
        WireSlider(transposeSlider, transposeValue, PianoConfig.SectionName, nameof(PianoConfig.Transpose));
        WireSlider(volumeSlider, volumeValue, AudioConfig.SectionName, nameof(AudioConfig.VolumeInPercent), val => $"{val}%");

        // Wire dropdowns
        WireComboBox(sustainModeCombo, UIConfig.SectionName, nameof(UIConfig.SustainMode));
        WireComboBox(noteNameStyleCombo, UIConfig.SectionName, nameof(UIConfig.NoteNameStyle));
        WireComboBox(keyboardLayoutCombo, UIConfig.SectionName, nameof(UIConfig.KeyboardLayout));

        // Wire toggles
        WireToggle(topmostToggle, UIConfig.SectionName, nameof(UIConfig.Topmost));
        WireToggle(keyLabelsToggle, UIConfig.SectionName, nameof(UIConfig.ShowKeyLabels));
        WireToggle(noteLabelsToggle, UIConfig.SectionName, nameof(UIConfig.ShowNoteLabels));

        // Sync controls when config reloads (covers reset + external changes)
        pianoConfig.OnChange(newPiano => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            velocitySlider.Value = newPiano.Velocity;
            transposeSlider.Value = newPiano.Transpose;
        }));

        audioConfig.OnChange(newAudio => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            volumeSlider.Value = newAudio.VolumeInPercent));

        uiConfig.OnChange(newUi => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            sustainModeCombo.SelectedItem = newUi.SustainMode;
            topmostToggle.IsChecked = newUi.Topmost;
            keyLabelsToggle.IsChecked = newUi.ShowKeyLabels;
            noteLabelsToggle.IsChecked = newUi.ShowNoteLabels;
            noteNameStyleCombo.SelectedItem = newUi.NoteNameStyle;
            keyboardLayoutCombo.SelectedItem = newUi.KeyboardLayout;
        }));

        // Wire reset
        ToolbarButton resetButton = new(_ResetDefaultsButtonLabel);
        resetButton.PointerPressed += (_, e) =>
        {
            _settingsService.ResetAll();
            e.Handled = true;
        };

        Child = new ScrollViewer
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
                    CreateRow(_SustainModeLabel, sustainModeCombo),
                    CreateRow(_TopmostLabel, topmostToggle),
                    CreateRow(_ShowKeyLabelsLabel, keyLabelsToggle),
                    CreateRow(_ShowNoteLabelsLabel, noteLabelsToggle),
                    CreateRow(_NoteNameStyleLabel, noteNameStyleCombo),
                    CreateRow(_KeyboardLayoutLabel, keyboardLayoutCombo),
                    CreateResetRow(resetButton),
                },
            },
        };
    }
}
