using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Controls;

public class KlavierButton : ActivableControl
{
    private readonly TextBlock _label;

    public KlavierButton(string text, bool momentaryActiveOnPress = true)
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

        if (momentaryActiveOnPress) // default button feedback
        {
            PointerPressed += (_, _) => IsActive = true;
            PointerReleased += (_, _) => IsActive = false;
        }
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        _label.Foreground = isActive ? ActiveTextBrush : DefaultTextBrush;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            Opacity = IsEnabled ? 1.0 : 0.5;
        }
    }
}
