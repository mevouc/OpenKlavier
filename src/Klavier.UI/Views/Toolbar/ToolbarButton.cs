using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Toolbar;

public class ToolbarButton : ActivableControl
{
    private readonly TextBlock _label;

    public ToolbarButton(string text)
    {
        _label = new TextBlock
        {
            Text = text,
            Foreground = DefaultTextBrush,
            FontSize = Constants.KeyLabelsFontSize,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Padding = new Thickness(12, 4);
        Child = _label;
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        _label.Foreground = isActive ? ActiveTextBrush : DefaultTextBrush;
    }
}
