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
    public required Color Accent { get; init; }
    public required Color Divider { get; init; }

    public Color AccentLight1 => Lighten(Accent, 0.15);
    public Color AccentLight2 => Lighten(Accent, 0.30);
    public Color AccentLight3 => Lighten(Accent, 0.45);
    public Color AccentDark1 => Darken(Accent, 0.15);
    public Color AccentDark2 => Darken(Accent, 0.30);
    public Color AccentDark3 => Darken(Accent, 0.45);
    public Color HoverHighlight => Mix(Accent, NeutralSurface, 0.15);

    private static Color Lighten(Color color, double amount)
    {
        return Color.FromRgb(
            ClampByte(color.R * (1 + amount)),
            ClampByte(color.G * (1 + amount)),
            ClampByte(color.B * (1 + amount)));
    }

    private static Color Darken(Color color, double amount)
    {
        return Color.FromRgb(
            ClampByte(color.R * (1 - amount)),
            ClampByte(color.G * (1 - amount)),
            ClampByte(color.B * (1 - amount)));
    }

    private static Color Mix(Color a, Color b, double aWeight)
    {
        return Color.FromRgb(
            ClampByte((a.R * aWeight) + (b.R * (1 - aWeight))),
            ClampByte((a.G * aWeight) + (b.G * (1 - aWeight))),
            ClampByte((a.B * aWeight) + (b.B * (1 - aWeight))));
    }

    private static byte ClampByte(double value)
    {
        return (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
    }
}
