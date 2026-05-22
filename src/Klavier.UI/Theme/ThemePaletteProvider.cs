using Avalonia.Media;
using Avalonia.Styling;

namespace Klavier.UI.Theme;

// Process-global by design: theme is set once at startup and changing it requires a restart.
public static class ThemePaletteProvider
{
    public static readonly ThemePalette Dark = new()
    {
        FluentVariant = ThemeVariant.Dark,
        Contrasted = Color.Parse("#101010"),
        Strong = Color.Parse("#1E1E1E"),
        Soft = Color.Parse("#2D2D2D"),
        Neutral = Color.Parse("#333333"),
        Inverse = Color.Parse("#E0E0E0"),
    };

    public static readonly ThemePalette Light = new()
    {
        FluentVariant = ThemeVariant.Light,
        Contrasted = Color.Parse("#FAFAFA"),
        Strong = Color.Parse("#F1F1F1"),
        Soft = Color.Parse("#E8E8E8"),
        Neutral = Color.Parse("#DDDDDD"),
        Inverse = Color.Parse("#1E1E1E"),
    };

    public static ThemePalette Active { get; private set; } = Dark;

    public static void SetActive(ThemePalette palette) => Active = palette;

    public static Color MediumContrasted => Active.Strong;
    public static Color Neutral => Active.Neutral;
    public static Color Contrasted => Active.Contrasted;
    public static Color Inverse => Active.Inverse;
    public static Color Medium => Active.Soft;
    public static Color Accent => UserPalette.Accent;
    public static Color AccentLight1 => UserPalette.AccentLight1;
    public static Color AccentLight2 => UserPalette.AccentLight2;
    public static Color AccentLight3 => UserPalette.AccentLight3;
    public static Color AccentDark1 => UserPalette.AccentDark1;
    public static Color AccentDark2 => UserPalette.AccentDark2;
    public static Color AccentDark3 => UserPalette.AccentDark3;
    public static Color AccentNeutral => ColorMath.Mix(UserPalette.Accent, Active.Neutral, 0.15);
    public static Color AccentContrasted => ColorMath.Mix(UserPalette.Accent, Active.Contrasted, 0.5);
}
