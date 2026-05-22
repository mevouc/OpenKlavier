using Avalonia.Media;
using Avalonia.Styling;

namespace Klavier.UI.Theme;

public class ThemePalette
{
    public required ThemeVariant FluentVariant { get; init; }
    public required Color Strong { get; init; }
    public required Color Neutral { get; init; }
    public required Color Contrasted { get; init; }
    public required Color Inverse { get; init; }
    public required Color Soft { get; init; }
}
