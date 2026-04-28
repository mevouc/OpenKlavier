using Avalonia.Media;
using Avalonia.Styling;

namespace Klavier.UI.Theme;

// Process-global by design: theme is set once at startup and changing it requires a restart.
public static class ThemePaletteProvider
{
    public static readonly ThemePalette Dark = new()
    {
        FluentVariant = ThemeVariant.Dark,
        AppBackground = Color.Parse("#1E1E1E"),
        NeutralSurface = Color.Parse("#333333"),
        ContrastedSurface = Color.Parse("#101010"),
        TextPrimary = Color.Parse("#E0E0E0"),
        Divider = Color.Parse("#2D2D2D"),
    };

    public static readonly ThemePalette Light = new()
    {
        FluentVariant = ThemeVariant.Light,
        AppBackground = Color.Parse("#F1F1F1"),
        NeutralSurface = Color.Parse("#DDDDDD"),
        ContrastedSurface = Color.Parse("#FAFAFA"),
        TextPrimary = Color.Parse("#1E1E1E"),
        Divider = Color.Parse("#E8E8E8"),
    };

    public static ThemePalette Active { get; private set; } = Dark;

    public static void SetActive(ThemePalette palette) => Active = palette;

    public static Color AppBackground => Active.AppBackground;
    public static Color NeutralSurface => Active.NeutralSurface;
    public static Color ContrastedSurface => Active.ContrastedSurface;
    public static Color TextPrimary => Active.TextPrimary;
    public static Color Divider => Active.Divider;
    public static Color Accent => UserPalette.Accent;
    public static Color AccentLight1 => UserPalette.AccentLight1;
    public static Color AccentLight2 => UserPalette.AccentLight2;
    public static Color AccentLight3 => UserPalette.AccentLight3;
    public static Color AccentDark1 => UserPalette.AccentDark1;
    public static Color AccentDark2 => UserPalette.AccentDark2;
    public static Color AccentDark3 => UserPalette.AccentDark3;
    public static Color HoverHighlight => ColorMath.Mix(UserPalette.Accent, Active.NeutralSurface, 0.15);
}
