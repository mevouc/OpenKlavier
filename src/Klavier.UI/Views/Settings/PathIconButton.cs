using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Klavier.UI.Views.Controls;

namespace Klavier.UI.Views.Settings;

public class PathIconButton : ActivableControl
{
    private readonly PathIcon _icon;

    public PathIconButton(Geometry geometry, double iconSize)
    {
        _icon = new PathIcon
        {
            Data = geometry,
            Width = iconSize,
            Height = iconSize,
            Foreground = DefaultTextBrush,
        };

        Padding = new Thickness(10, 0);
        Child = _icon;
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        _icon.Foreground = isActive ? ActiveTextBrush : DefaultTextBrush;
    }
}
