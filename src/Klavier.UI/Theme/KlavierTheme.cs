using Avalonia.Media;

namespace Klavier.UI.Theme;

public static class KlavierTheme
{
    public static readonly Color AppBackground = Color.Parse("#1E1E1E");
    public static readonly Color PanelBackground = Color.Parse("#2D2D2D");
    public static readonly Color TextPrimary = Color.Parse("#E0E0E0");
    public static readonly Color Accent = Color.Parse("#3A60BF");
    public static readonly Color AccentLight1 = Lighten(Accent, 0.15);
    public static readonly Color AccentLight2 = Lighten(Accent, 0.30);
    public static readonly Color AccentLight3 = Lighten(Accent, 0.45);
    public static readonly Color AccentDark1 = Darken(Accent, 0.15);
    public static readonly Color AccentDark2 = Darken(Accent, 0.30);
    public static readonly Color AccentDark3 = Darken(Accent, 0.45);
    public static readonly Color Divider = Color.Parse("#333333");

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
