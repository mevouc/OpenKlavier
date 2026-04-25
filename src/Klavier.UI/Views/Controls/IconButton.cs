using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Controls;

public class IconButton : CustomButtonBase
{
    private readonly PathIcon _icon;

    public IconButton(Geometry glyph, bool momentaryActiveOnPress = true)
        : base(momentaryActiveOnPress)
    {
        _icon = new PathIcon
        {
            Data = glyph,
            Width = Constants.IconSize,
            Height = Constants.IconSize,
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
