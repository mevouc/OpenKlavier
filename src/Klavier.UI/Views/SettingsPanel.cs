using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.UI.Ports;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class SettingsPanel : Border
{
    private static readonly SolidColorBrush _TextBrush = new(KlavierTheme.TextPrimary);
    private const double _LabelWidth = 130;
    private const double _ValueWidth = 40;
    private const double _MinRowHeight = 32;


    public SettingsPanel(
        IUserSettingsService settingsService,
        IOptionsMonitor<PianoConfig> pianoConfig,
        IOptionsMonitor<AudioConfig> audioConfig,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        Background = new SolidColorBrush(KlavierTheme.AppBackground);
        Padding = new Thickness(12, 8);
        IsVisible = false;

        PianoConfig piano = pianoConfig.CurrentValue;
        AudioConfig audio = audioConfig.CurrentValue;
        UIConfig ui = uiConfig.CurrentValue;

        Slider velocitySlider = CreateSlider(0, 127, piano.Velocity);
        TextBlock velocityValue = CreateValueLabel(piano.Velocity.ToString());

        Slider transposeSlider = CreateSlider(-24, 24, piano.Transpose);
        TextBlock transposeValue = CreateValueLabel(piano.Transpose.ToString());

        Slider volumeSlider = CreateSlider(0, 120, audio.VolumeInPercent);
        TextBlock volumeValue = CreateValueLabel($"{audio.VolumeInPercent}%");

        ComboBox sustainModeCombo = CreateComboBox<SustainMode>(ui.SustainMode);
        ToggleSwitch topmostToggle = CreateToggleSwitch(ui.Topmost);
        ToggleSwitch keyLabelsToggle = CreateToggleSwitch(ui.ShowKeyLabels);
        ToggleSwitch noteLabelsToggle = CreateToggleSwitch(ui.ShowNoteLabels);
        ComboBox noteNameStyleCombo = CreateComboBox<NoteNameStyle>(ui.NoteNameStyle);
        ComboBox keyboardLayoutCombo = CreateComboBox(
            ["qwerty", "azerty", "dvorak-fr"],
            ui.KeyboardLayout);

        // Wire sliders
        velocitySlider.ValueChanged += (_, e) =>
        {
            int val = (int)e.NewValue;
            velocityValue.Text = val.ToString();
            settingsService.UpdateSetting("Piano", "Velocity", val);
        };

        transposeSlider.ValueChanged += (_, e) =>
        {
            int val = (int)e.NewValue;
            transposeValue.Text = val.ToString();
            settingsService.UpdateSetting("Piano", "Transpose", val);
        };

        volumeSlider.ValueChanged += (_, e) =>
        {
            int val = (int)e.NewValue;
            volumeValue.Text = $"{val}%";
            settingsService.UpdateSetting("Audio", "VolumeInPercent", val);
        };

        // Wire dropdowns
        sustainModeCombo.SelectionChanged += (_, _) =>
        {
            if (sustainModeCombo.SelectedItem is SustainMode mode)
            {
                settingsService.UpdateSetting("UI", "SustainMode", mode.ToString());
            }
        };

        noteNameStyleCombo.SelectionChanged += (_, _) =>
        {
            if (noteNameStyleCombo.SelectedItem is NoteNameStyle style)
            {
                settingsService.UpdateSetting("UI", "NoteNameStyle", style.ToString());
            }
        };

        keyboardLayoutCombo.SelectionChanged += (_, _) =>
        {
            if (keyboardLayoutCombo.SelectedItem is string layout)
            {
                settingsService.UpdateSetting("UI", "KeyboardLayout", layout);
            }
        };

        // Wire toggles
        topmostToggle.IsCheckedChanged += (_, _) =>
            settingsService.UpdateSetting("UI", "Topmost", topmostToggle.IsChecked == true);

        keyLabelsToggle.IsCheckedChanged += (_, _) =>
            settingsService.UpdateSetting("UI", "ShowKeyLabels", keyLabelsToggle.IsChecked == true);

        noteLabelsToggle.IsCheckedChanged += (_, _) =>
            settingsService.UpdateSetting("UI", "ShowNoteLabels", noteLabelsToggle.IsChecked == true);

        // Wire reset
        ToolbarButton resetButton = new("Reset Defaults");
        resetButton.PointerPressed += (_, e) =>
        {
            settingsService.ResetAll();

            // Reset UI controls to current config values
            PianoConfig resetPiano = pianoConfig.CurrentValue;
            AudioConfig resetAudio = audioConfig.CurrentValue;
            UIConfig resetUi = uiConfig.CurrentValue;

            velocitySlider.Value = resetPiano.Velocity;
            transposeSlider.Value = resetPiano.Transpose;
            volumeSlider.Value = resetAudio.VolumeInPercent;
            sustainModeCombo.SelectedItem = resetUi.SustainMode;
            topmostToggle.IsChecked = resetUi.Topmost;
            keyLabelsToggle.IsChecked = resetUi.ShowKeyLabels;
            noteLabelsToggle.IsChecked = resetUi.ShowNoteLabels;
            noteNameStyleCombo.SelectedItem = resetUi.NoteNameStyle;
            keyboardLayoutCombo.SelectedItem = resetUi.KeyboardLayout;

            e.Handled = true;
        };

        Child = new ScrollViewer
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
                Children =
                {
                    CreateRow("Velocity", velocityValue, velocitySlider),
                    CreateRow("Transpose", transposeValue, transposeSlider),
                    CreateRow("Volume", volumeValue, volumeSlider),
                    CreateRow("Sustain Mode", sustainModeCombo),
                    CreateRow("Topmost", topmostToggle),
                    CreateRow("Show Key Labels", keyLabelsToggle),
                    CreateRow("Show Note Labels", noteLabelsToggle),
                    CreateRow("Note Name Style", noteNameStyleCombo),
                    CreateRow("Keyboard Layout", keyboardLayoutCombo),
                    CreateResetRow(resetButton),
                },
            },
        };
    }

    private static DockPanel CreateRow(string label, Control control)
    {
        return new DockPanel
        {
            MinHeight = _MinRowHeight,
            Children =
            {
                CreateLabel(label),
                control,
            },
        };
    }

    private static DockPanel CreateRow(string label, TextBlock valueLabel, Slider slider)
    {
        DockPanel.SetDock(valueLabel, Dock.Left);

        return new DockPanel
        {
            MinHeight = _MinRowHeight,
            Children =
            {
                CreateLabel(label),
                valueLabel,
                slider,
            },
        };
    }

    private static DockPanel CreateResetRow(ToolbarButton button)
    {
        button.HorizontalAlignment = HorizontalAlignment.Right;
        button.Margin = new Thickness(0, 4, 0, 0);

        DockPanel panel = new();
        DockPanel.SetDock(button, Dock.Right);
        panel.Children.Add(button);

        return panel;
    }

    private static TextBlock CreateLabel(string text)
    {
        TextBlock label = new()
        {
            Text = text,
            Foreground = _TextBrush,
            FontSize = Constants.KeyLabelsFontSize,
            Width = _LabelWidth,
            VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(label, Dock.Left);

        return label;
    }

    private static TextBlock CreateValueLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = _TextBrush,
            FontSize = Constants.KeyLabelsFontSize,
            Width = _ValueWidth,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
    }

    private static Slider CreateSlider(double min, double max, double value)
    {
        return new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, -12, 0, -12),
        };
    }

    private static ComboBox CreateComboBox<TEnum>(TEnum selectedValue) where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        ComboBox comboBox = new()
        {
            ItemsSource = values,
            SelectedItem = selectedValue,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 120,
            Focusable = false,
        };

        return comboBox;
    }

    private static ComboBox CreateComboBox(string[] items, string selectedValue)
    {
        ComboBox comboBox = new()
        {
            ItemsSource = items,
            SelectedItem = selectedValue,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 120,
            Focusable = false,
        };

        return comboBox;
    }

    private static ToggleSwitch CreateToggleSwitch(bool isOn)
    {
        return new ToggleSwitch
        {
            IsChecked = isOn,
            VerticalAlignment = VerticalAlignment.Center,
            OnContent = null,
            OffContent = null,
            Focusable = false,
        };
    }

}
