using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config.Schema;
using Klavier.SoundFont;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Settings.KeybindsEditor;

namespace Klavier.UI.Views.Settings;

public partial class SettingsView
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

    private const string _SoundFontPickerTitle = "Choose a SoundFont file";

    private static DockPanel CreateRow(string label, Control control, string? tooltip = null)
    {
        DockPanel row = new()
        {
            MinHeight = _MinRowHeight,
            Margin = new Thickness(_RowIndent, 0, 0, 0),
            Children =
            {
                CreateLabel(label),
                control,
            },
        };
        if (tooltip is not null)
        {
            ToolTip.SetTip(row, tooltip);
        }
        return row;
    }

    private static DockPanel CreateRow(string label, TextBlock valueLabel, Slider slider, string? tooltip = null)
    {
        DockPanel.SetDock(valueLabel, Dock.Left);

        DockPanel row = new()
        {
            MinHeight = _MinRowHeight,
            Margin = new Thickness(_RowIndent, 0, 0, 0),
            Children =
            {
                CreateLabel(label),
                valueLabel,
                slider,
            },
        };
        if (tooltip is not null)
        {
            ToolTip.SetTip(row, tooltip);
        }
        return row;
    }

    private static DockPanel CreateResetRow(TextButton button)
    {
        button.HorizontalAlignment = HorizontalAlignment.Right;
        button.Margin = new Thickness(0, 4, 0, 0);

        DockPanel panel = new();
        DockPanel.SetDock(button, Dock.Right);
        panel.Children.Add(button);

        return panel;
    }

    private static Control CreateSectionHeader(string title, string? subtext = null)
    {
        TextBlock titleBlock = new()
        {
            Text = title,
            Foreground = _TextBrush,
            FontSize = Constants.PrimaryFontSize + 2,
            FontWeight = FontWeight.Bold,
        };

        if (subtext is null)
        {
            titleBlock.Margin = new Thickness(0, 14, 0, 6);
            return titleBlock;
        }

        TextBlock subtextBlock = new()
        {
            Text = subtext,
            Foreground = _SubtextBrush,
            FontSize = Constants.PrimaryFontSize - 1,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8, 0, 0, 2),
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 14, 0, 6),
            Children = { titleBlock, subtextBlock },
        };
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

    private static StyledComboBox CreateComboBox<TEnum>(TEnum selectedValue) where TEnum : struct, Enum
        => CreateComboBox(Enum.GetValues<TEnum>(), selectedValue);

    private static StyledComboBox CreateComboBox(System.Collections.IEnumerable items, object? selectedValue) => new()
    {
        ItemsSource = items,
        SelectedItem = selectedValue,
    };

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

    private static IconButton CreatePlusIconButton()
    {
        // Plus glyph (Material Icons "plus", 24x24 viewport).
        Geometry plusGeometry = Geometry.Parse(
            "M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z");
        return new IconButton(plusGeometry);
    }

    private static IconButton CreatePencilIconButton()
    {
        // Pencil glyph (Material Icons "pencil", 24x24 viewport).
        Geometry pencilGeometry = Geometry.Parse(
            "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z");
        return new IconButton(pencilGeometry);
    }

    private void WireKeybindsEditorButton(IconButton button, bool useCurrentLayoutName)
    {
        button.PointerPressed += async (_, e) =>
        {
            e.Handled = true;
            try
            {
                await OpenKeybindsEditor(useCurrentLayoutName);
            }
            finally
            {
                button.IsActive = false;
            }
        };
    }

    private async Task OpenKeybindsEditor(bool useCurrentLayoutName)
    {
        string currentLayout = _uiConfig.CurrentValue.KeyboardLayout;
        KeyboardMapping clone = _keyboardMappingService.Load(currentLayout);
        string? existingLayoutName = useCurrentLayoutName ? currentLayout : null;
        KeybindsEditorWindow editor = _createKeybindsEditor(clone, existingLayoutName);

        Window? parent = TopLevel.GetTopLevel(this) as Window;
        if (parent is not null)
        {
            await editor.ShowDialog(parent);
        }
        else
        {
            editor.Show();
        }
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
