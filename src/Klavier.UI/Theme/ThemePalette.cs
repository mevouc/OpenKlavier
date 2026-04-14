using Avalonia.Media;
using Avalonia.Styling;

namespace Klavier.UI.Theme;

public class ThemePalette
{
    public required ThemeVariant FluentVariant { get; init; }
    public required Color AppBackground { get; init; }
    public required Color PanelBackground { get; init; }
    public required Color TextPrimary { get; init; }
    public required Color Accent { get; init; }
    public required Color Divider { get; init; }

    public Color AccentLight1 => Lighten(Accent, 0.15);
    public Color AccentLight2 => Lighten(Accent, 0.30);
    public Color AccentLight3 => Lighten(Accent, 0.45);
    public Color AccentDark1 => Darken(Accent, 0.15);
    public Color AccentDark2 => Darken(Accent, 0.30);
    public Color AccentDark3 => Darken(Accent, 0.45);

    private static Color Lighten(Color color, double amount)
    {
        return Color.FromRgb(
            (byte)(color.R * (1 + amount)),
            (byte)(color.G * (1 + amount)),
            (byte)(color.B * (1 + amount)));
    }

    private static Color Darken(Color color, double amount)
    {
        return Color.FromRgb(
            (byte)(color.R * (1 - amount)),
            (byte)(color.G * (1 - amount)),
            (byte)(color.B * (1 - amount)));
    }
}
