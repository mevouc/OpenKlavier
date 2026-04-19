using Avalonia.Media;

namespace Klavier.UI.Theme;

public static class ColorMath
{
    public static Color Lighten(Color color, double amount)
    {
        return Color.FromRgb(
            ClampByte(color.R * (1 + amount)),
            ClampByte(color.G * (1 + amount)),
            ClampByte(color.B * (1 + amount)));
    }

    public static Color Darken(Color color, double amount)
    {
        return Color.FromRgb(
            ClampByte(color.R * (1 - amount)),
            ClampByte(color.G * (1 - amount)),
            ClampByte(color.B * (1 - amount)));
    }

    public static Color Mix(Color a, Color b, double aWeight)
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
