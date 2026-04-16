using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Toolbar;

public class ToolbarView : Border
{
    private bool _isSettingsOpen;

    public event Action<bool>? SettingsToggled;

    public ToolbarView(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig)
    {
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Padding = new Thickness(8, 4);

        ToolbarButton panicButton = new("Panic");
        panicButton.PointerPressed += (_, e) =>
        {
            pianoEngine.Panic();
            if (uiConfig.CurrentValue.SustainMode == SustainMode.InvertedHold)
            {
                pianoEngine.SustainOn();
            }
            panicButton.IsActive = true;
            e.Handled = true;
        };
        panicButton.PointerReleased += (_, e) =>
        {
            panicButton.IsActive = false;
            e.Handled = true;
        };

        ToolbarButton settingsButton = new("Settings") { Margin = new Thickness(4, 0, 0, 0) };
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
