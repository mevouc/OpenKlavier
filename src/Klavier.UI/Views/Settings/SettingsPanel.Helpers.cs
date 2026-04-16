using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;
using Klavier.UI.Views.Toolbar;

namespace Klavier.UI.Views;

public partial class SettingsPanel
{
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
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, -12, 0, -12),
            Focusable = false,
        };
    }

    private static ComboBox CreateComboBox<TEnum>(TEnum selectedValue) where TEnum : struct, Enum
        => CreateComboBox(Enum.GetValues<TEnum>(), selectedValue);

    private static ComboBox CreateComboBox(System.Collections.IEnumerable items, object selectedValue)
    {
        return new ComboBox
        {
            ItemsSource = items,
            SelectedItem = selectedValue,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 120,
            Focusable = false,
        };
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

    private void WireSlider(
        Slider slider, TextBlock valueLabel,
        string section, string key,
        Func<int, string>? formatter = null)
    {
        slider.ValueChanged += (_, e) =>
        {
            int val = (int)e.NewValue;
            valueLabel.Text = formatter?.Invoke(val) ?? val.ToString();
            _settingsService.UpdateSetting(section, key, val);
        };
    }

    private void WireComboBox(
        ComboBox comboBox,
        string section, string key)
    {
        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is { } value)
            {
                _settingsService.UpdateSetting(section, key, value.ToString()!);
            }
        };
    }

    private void WireToggle(
        ToggleSwitch toggle,
        string section, string key)
    {
        toggle.IsCheckedChanged += (_, _) =>
            _settingsService.UpdateSetting(section, key, toggle.IsChecked == true);
    }
}
