using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Klavier.UI.Views.Controls;

public class IconButton : CustomButtonBase
{
    private readonly PathIcon _icon;

    public IconButton(Geometry glyph, double iconSize, bool momentaryActiveOnPress = true)
        : base(momentaryActiveOnPress)
    {
        _icon = new PathIcon
        {
            Data = glyph,
            Width = iconSize,
            Height = iconSize,
            Foreground = DefaultTextBrush,
        };
        Padding = new Thickness(10, 0);
        Child = _icon;
    }

    public Geometry? Glyph
    {
        get => _icon.Data;
        set => _icon.Data = value;
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        _icon.Foreground = isActive ? ActiveTextBrush : DefaultTextBrush;
    }
}
