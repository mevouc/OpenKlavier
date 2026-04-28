using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Controls;

public class TextButton : BaseButton
{
    private readonly TextBlock _label;

    public TextButton(string text, bool momentaryActiveOnPress = true)
        : base(momentaryActiveOnPress)
    {
        _label = new TextBlock
        {
            Text = text,
            Foreground = DefaultTextBrush,
            FontSize = Constants.PrimaryFontSize,
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
