using Avalonia.Media;
using Avalonia.Styling;

namespace Klavier.UI.Theme;

public class ThemePalette
{
    public required ThemeVariant FluentVariant { get; init; }
    public required Color AppBackground { get; init; }
    public required Color NeutralSurface { get; init; }
    public required Color ContrastedSurface { get; init; }
    public required Color TextPrimary { get; init; }
    public required Color Divider { get; init; }
}
