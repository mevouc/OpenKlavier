using Avalonia.Media;
using Avalonia.Styling;

namespace Klavier.UI.Theme;

public static class ThemePaletteProvider
{
    public static readonly ThemePalette Dark = new()
    {
        FluentVariant = ThemeVariant.Dark,
        AppBackground = Color.Parse("#1E1E1E"),
        NeutralSurface = Color.Parse("#333333"),
        ContrastedSurface = Color.Parse("#101010"),
        TextPrimary = Color.Parse("#E0E0E0"),
        Accent = Color.Parse("#3A60BF"),
        Divider = Color.Parse("#2D2D2D"),
    };

    public static readonly ThemePalette Light = new()
    {
        FluentVariant = ThemeVariant.Light,
        AppBackground = Color.Parse("#F1F1F1"),
        NeutralSurface = Color.Parse("#DDDDDD"),
        ContrastedSurface = Color.Parse("#FAFAFA"),
        TextPrimary = Color.Parse("#1E1E1E"),
        Accent = Color.Parse("#3A60BF"),
        Divider = Color.Parse("#E8E8E8"),
    };

    public static ThemePalette Active { get; private set; } = Dark;

    public static void SetActive(ThemePalette palette) => Active = palette;

    public static Color AppBackground => Active.AppBackground;
    public static Color NeutralSurface => Active.NeutralSurface;
    public static Color ContrastedSurface => Active.ContrastedSurface;
    public static Color TextPrimary => Active.TextPrimary;
    public static Color Accent => Active.Accent;
    public static Color Divider => Active.Divider;
    public static Color AccentLight1 => Active.AccentLight1;
    public static Color AccentLight2 => Active.AccentLight2;
    public static Color AccentLight3 => Active.AccentLight3;
    public static Color AccentDark1 => Active.AccentDark1;
    public static Color AccentDark2 => Active.AccentDark2;
    public static Color AccentDark3 => Active.AccentDark3;
    public static Color HoverHighlight => Active.HoverHighlight;
}
