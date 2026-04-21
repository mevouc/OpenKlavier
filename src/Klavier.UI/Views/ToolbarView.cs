using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class ToolbarView : Border
{
    private bool _isSettingsOpen;

    public event Action<bool>? SettingsToggled;

    public ToolbarView(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig)
    {
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Padding = new Thickness(8, 4);

        KlavierButton panicButton = new("Panic");
        panicButton.PointerPressed += (_, e) =>
        {
            pianoEngine.Panic();
            if (uiConfig.CurrentValue.SustainMode == SustainMode.InvertedHold)
            {
                pianoEngine.SustainOn();
            }
            e.Handled = true;
        };

        KlavierButton settingsButton = new("Settings", momentaryActiveOnPress: false) { Margin = new Thickness(4, 0, 0, 0) };
        settingsButton.PointerPressed += (_, e) =>
        {
            _isSettingsOpen = !_isSettingsOpen;
            settingsButton.IsActive = _isSettingsOpen;
            SettingsToggled?.Invoke(_isSettingsOpen);
            e.Handled = true;
        };

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { panicButton, settingsButton },
        };
    }
}
