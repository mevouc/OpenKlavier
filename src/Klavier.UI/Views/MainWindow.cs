using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Klavier.UI.Input;
using Klavier.UI.Options;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private readonly KeyboardInputHandler _keyboardInput;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _keyboardInput = keyboardInput;

        Title = "Klavier";
        Width = 1000;
        Height = 300;
        Background = new SolidColorBrush(KlavierTheme.AppBackground);
        Topmost = uiConfig.CurrentValue.Topmost;

        uiConfig.OnChange(config => Topmost = config.Topmost);

        Content = pianoView;
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
