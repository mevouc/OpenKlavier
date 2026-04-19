using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.Config;
using Klavier.SoundFont;
using Klavier.UI.Theme;
using Klavier.UI.Views.Settings;
using Klavier.UI.Views.Toolbar;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public partial class SettingsPanel
{
    private const string _SoundFontPickerTitle = "Choose a SoundFont file";

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
            FontSize = Constants.PrimaryFontSize,
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
            FontSize = Constants.PrimaryFontSize,
            Width = _ValueWidth,
            TextAlignment = TextAlignment.Left,
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
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, -12, 0, -12),
            Focusable = false,
        };
    }

    private static ComboBox CreateComboBox<TEnum>(TEnum selectedValue) where TEnum : struct, Enum
        => CreateComboBox(Enum.GetValues<TEnum>(), selectedValue);

    private static ComboBox CreateComboBox(System.Collections.IEnumerable items, object? selectedValue)
    {
        ComboBox comboBox = new()
        {
            ItemsSource = items,
            SelectedItem = selectedValue,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 120,
            Focusable = false,
            Background = _ContrastedSurfaceBrush,
            BorderBrush = _NeutralSurfaceBrush,
        };
        comboBox.Resources["ComboBoxBorderBrushPointerOver"] = _HoverHighlightBrush;
        return comboBox;
    }

    private static ToggleSwitch CreateToggleSwitch(bool isOn)
    {
        ToggleSwitch toggle = new()
        {
            IsChecked = isOn,
            VerticalAlignment = VerticalAlignment.Center,
            OnContent = null,
            OffContent = null,
            Focusable = false,
        };
        toggle.Resources["ToggleSwitchFillOff"] = _ContrastedSurfaceBrush;
        toggle.Resources["ToggleSwitchStrokeOff"] = _NeutralSurfaceBrush;
        toggle.Resources["ToggleSwitchStrokeOffPointerOver"] = _HoverHighlightBrush;
        return toggle;
    }

    private void WireSlider(
        Slider slider, TextBlock valueLabel,
        string keyPath,
        Func<int, string>? formatter = null)
    {
        slider.ValueChanged += (_, e) =>
        {
            int val = (int)e.NewValue;
            valueLabel.Text = formatter?.Invoke(val) ?? val.ToString();
            _settingsService.UpdateSetting(keyPath, val);
        };
    }

    private void WireComboBox(
        ComboBox comboBox,
        string keyPath)
    {
        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is { } value)
            {
                _settingsService.UpdateSetting(keyPath, value.ToString()!);
            }
        };
    }

    private void WireToggle(
        ToggleSwitch toggle,
        string keyPath)
    {
        toggle.IsCheckedChanged += (_, _) =>
            _settingsService.UpdateSetting(keyPath, toggle.IsChecked == true);
    }

    private void WireHexColorTextBox(TextBox textBox, string keyPath)
    {
        textBox.TextChanged += (_, _) =>
        {
            if (!textBox.IsFocused)
            {
                return;
            }
            if (TryParseHex(textBox.Text, out _))
            {
                _settingsService.UpdateSetting(keyPath, textBox.Text!);
            }
        };
    }

    private void WirePresetComboBox(ComboBox comboBox, string presetKeyPath)
    {
        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is SoundFontPreset preset)
            {
                _settingsService.UpdateSetting(presetKeyPath, new { preset.Bank, preset.Program });
            }
        };
    }

    private static SoundFontPreset? FindPreset(
        IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> presets,
        SoundFontPresetConfig config)
    {
        return presets.TryGetValue((config.Bank, config.Program), out SoundFontPreset preset)
            ? preset
            : null;
    }

    private static TextBox CreateSoundFontPathDisplay(string displayText, string? tooltip)
    {
        TextBox textBox = new()
        {
            Text = displayText,
            IsReadOnly = true,
            Focusable = false,
            Foreground = _TextBrush,
            FontSize = Constants.PrimaryFontSize,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0),
        };
        ToolTip.SetTip(textBox, tooltip);
        return textBox;
    }

    private static PathIconButton CreateSoundFontPickerButton()
    {
        // Folder glyph (Material Icons "folder", 24x24 viewport).
        Geometry folderGeometry = Geometry.Parse(
            "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z");
        return new PathIconButton(folderGeometry, iconSize: 14)
        {
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    private static Border CreateSoundFontPickerControl(TextBox pathDisplay, Border pickerButton)
    {
        DockPanel.SetDock(pickerButton, Dock.Right);
        return new Border
        {
            Background = _ContrastedSurfaceBrush,
            BorderBrush = _NeutralSurfaceBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(Constants.CornerRadius),
            ClipToBounds = true,
            MinWidth = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new DockPanel
            {
                LastChildFill = true,
                Children = { pickerButton, pathDisplay },
            },
        };
    }

    private static (string Display, string? Tooltip) GetSoundFontDisplayName(string? soundFontName, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(soundFontName))
        {
            return (Display: soundFontName, Tooltip: filePath);
        }
        return (Display: Path.GetFileName(filePath), Tooltip: null);
    }

    private void WireSoundFontPicker(PathIconButton pickerButton, IOptionsMonitor<AudioConfig> audioConfig)
    {
        pickerButton.PointerPressed += async (_, e) =>
        {
            e.Handled = true;
            pickerButton.IsActive = true;
            await HandleSoundFontPicker(pickerButton, audioConfig);
            pickerButton.IsActive = false;
        };
    }

    private async Task HandleSoundFontPicker(PathIconButton pickerButton, IOptionsMonitor<AudioConfig> audioConfig)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(pickerButton);
        if (topLevel is null)
        {
            return;
        }

        SoundFontConfig soundFontConfig = audioConfig.CurrentValue.SoundFont;

        IStorageFolder? suggestedFolder = null;
        string? currentDir = Path.GetDirectoryName(soundFontConfig.Path);
        if (!string.IsNullOrEmpty(currentDir))
        {
            suggestedFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(currentDir);
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _SoundFontPickerTitle,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("SoundFont")
                    {
                        Patterns = ["*.sf2", "*.sf3"],
                    }],
            SuggestedStartLocation = suggestedFolder,
        });
        if (files.Count == 0)
        {
            return;
        }
        string newPath = files[0].Path.LocalPath;

        SoundFontInfo newInfo;
        try
        {
            newInfo = SoundFontParser.ParseInfo(newPath);
        }
        catch (InvalidDataException)
        {
            return; // not a soundfont file, abort file update
        }

        (int newBank, int newProgram) = DetermineNewPreset(newInfo.Presets, soundFontConfig.Preset);

        _settingsService.UpdateSetting(
            ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont)),
            new { Path = newPath, Preset = new { newBank, newProgram } });
    }

    // Keep the current (Bank, Program) if still present in the new SF; otherwise pick (0, 0)
    // when available, else the lowest available preset key.
    private static (int Bank, int Program) DetermineNewPreset(
        IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> presets,
        SoundFontPresetConfig current)
    {
        (int Bank, int Program) currentKey = (current.Bank, current.Program);
        if (presets.ContainsKey(currentKey))
        {
            return currentKey;
        }
        if (presets.ContainsKey((0, 0)) || presets.Count == 0)
        {
            return (0, 0);
        }
        return presets.Keys.Min();
    }

    private static TextBox CreateHexColorTextBox(Color initialColor)
    {
        string lastValidHex = FormatHex(initialColor);
        TextBox textBox = new()
        {
            Text = lastValidHex,
            MaxLength = 7,
            MinWidth = 100,
            FontSize = Constants.PrimaryFontSize,
            BorderBrush = _NeutralSurfaceBrush,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        textBox.Resources["TextControlBorderBrushPointerOver"] = _HoverHighlightBrush;
        ApplyHexTextBoxColor(textBox, initialColor);

        textBox.TextChanged += (_, _) =>
        {
            if (TryParseHex(textBox.Text, out Color parsed))
            {
                lastValidHex = textBox.Text!;
                ApplyHexTextBoxColor(textBox, parsed);
            }
        };
        textBox.LostFocus += (_, _) =>
        {
            if (!TryParseHex(textBox.Text, out _))
            {
                textBox.Text = lastValidHex;
            }
            UpdateHexTextBoxForeground(textBox);
        };
        textBox.GotFocus += (_, _) => UpdateHexTextBoxForeground(textBox);
        textBox.PointerEntered += (_, _) => UpdateHexTextBoxForeground(textBox);
        textBox.PointerExited += (_, _) => UpdateHexTextBoxForeground(textBox);
        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                TopLevel.GetTopLevel(textBox)?.Focus();
                e.Handled = true;
            }
        };

        return textBox;
    }

    private static void ApplyHexTextBoxColor(TextBox textBox, Color color)
    {
        SolidColorBrush normalBg = new(color);
        SolidColorBrush normalFg = new(GetContrastingTextColor(color));
        textBox.Background = normalBg;
        textBox.SelectionBrush = normalFg;
        textBox.SelectionForegroundBrush = normalBg;
        textBox.Resources["TextControlBackgroundPointerOver"] = normalFg;
        textBox.Resources["TextControlBackgroundFocused"] = normalBg;
        UpdateHexTextBoxForeground(textBox);
    }

    private static void UpdateHexTextBoxForeground(TextBox textBox)
    {
        bool swap = textBox.IsPointerOver && !textBox.IsFocused;
        textBox.Foreground = swap ? textBox.Background : textBox.SelectionBrush;
    }

    private static string FormatHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        if (text is null || text.Length != 7 || text[0] != '#')
        {
            return false;
        }
        return Color.TryParse(text, out color);
    }

    private static Color GetContrastingTextColor(Color background)
    {
        double luminance = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
        Color towards = luminance < 128 ? Colors.White : Colors.Black;
        return ColorMath.Mix(background, towards, 0.35);
    }
}
