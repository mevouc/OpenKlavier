using Avalonia.Media;
using Klavier.Config.Schema;

namespace Klavier.UI.Theme;

// Process-global by design: colors are loaded once at startup and changing them requires a restart.
public static class UserPalette
{
    private static Color _accent;
    private static Color _whiteKey;
    private static Color _blackKey;
    private static Color _keyBorder;

    public static Color Accent => _accent;
    public static Color WhiteKey => _whiteKey;
    public static Color BlackKey => _blackKey;
    public static Color KeyBorder => _keyBorder;

    public static Color AccentLight1 => ColorMath.Lighten(Accent, 0.15);
    public static Color AccentLight2 => ColorMath.Lighten(Accent, 0.30);
    public static Color AccentLight3 => ColorMath.Lighten(Accent, 0.45);
    public static Color AccentDark1 => ColorMath.Darken(Accent, 0.15);
    public static Color AccentDark2 => ColorMath.Darken(Accent, 0.30);
    public static Color AccentDark3 => ColorMath.Darken(Accent, 0.45);

    public static Color WhiteKeyPressed => ColorMath.Mix(WhiteKey, Accent, 0.64);
    public static Color BlackKeyPressed => ColorMath.Mix(BlackKey, Accent, 0.72);

    public static void Initialize(ColorsConfig config)
    {
        _accent = Color.Parse(config.Accent);
        _whiteKey = Color.Parse(config.WhiteKey);
        _blackKey = Color.Parse(config.BlackKey);
        _keyBorder = Color.Parse(config.KeyBorder);
    }
}
