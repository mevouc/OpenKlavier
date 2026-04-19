using Avalonia.Media;

namespace Klavier.UI.Theme;

public static class PianoColors
{
    public static readonly Color WhiteKey = Color.Parse("#FAFAFA");
    public static readonly Color BlackKey = Color.Parse("#1C1C1C");
    public static readonly Color KeyBorder = Color.Parse("#333333");

    public static Color WhiteKeyPressed => ThemePalette.Mix(WhiteKey, ThemePaletteProvider.Accent, 0.64);
    public static Color BlackKeyPressed => ThemePalette.Mix(BlackKey, ThemePaletteProvider.Accent, 0.72);
}
