using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Klavier.UI.Input;
using Klavier.Config;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private const string _WindowTitle = "Klavier";
    private const int _DefaultWidth = 1000;
    private const int _DefaultHeight = 300;
    private const int _MinWidth = 700;
    private const int _MinHeight = 150;

    private readonly KeyboardInputHandler _keyboardInput;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        ToolbarView toolbarView,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _keyboardInput = keyboardInput;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(KlavierTheme.AppBackground);
        Topmost = uiConfig.CurrentValue.Topmost;

        uiConfig.OnChange(config => Topmost = config.Topmost);

        Border separator = new()
        {
            Height = 1,
            Background = new SolidColorBrush(KlavierTheme.Divider),
        };

        DockPanel.SetDock(toolbarView, Dock.Bottom);
        DockPanel.SetDock(separator, Dock.Bottom);

        Content = new DockPanel
        {
            Children = { toolbarView, separator, pianoView },
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = _keyboardInput.HandleKeyDown(e.PhysicalKey, e.KeyModifiers);

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        e.Handled = _keyboardInput.HandleKeyUp(e.PhysicalKey);

        base.OnKeyUp(e);
    }
}
