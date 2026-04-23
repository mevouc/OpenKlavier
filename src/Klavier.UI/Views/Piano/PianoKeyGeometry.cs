using System.Collections.Frozen;
using Klavier.Core.Primitives;

namespace Klavier.UI.Views.Piano;

public static class PianoKeyGeometry
{
    public const double BlackKeyWidthRatio = 0.68;

    private static readonly FrozenDictionary<NotePitch, int> _WhiteIndexByPitch = BuildWhiteIndexByPitch();

    public static int WhiteKeyCount => _WhiteIndexByPitch.Count;

    public static double GetColumnLeftX(NotePitch pitch, double panelWidth)
    {
        double whiteKeyWidth = panelWidth / WhiteKeyCount;

        if (!pitch.IsAccidental)
        {
            return _WhiteIndexByPitch[pitch] * whiteKeyWidth;
        }

        // Black key is centered on the boundary after its preceding white key
        NotePitch precedingWhite = new((ushort)(pitch.Value - 1));
        int precedingIndex = _WhiteIndexByPitch[precedingWhite];
        double blackKeyWidth = whiteKeyWidth * BlackKeyWidthRatio;
        return ((precedingIndex + 1) * whiteKeyWidth) - (blackKeyWidth / 2);
    }

    public static double GetColumnWidth(NotePitch pitch, double panelWidth)
    {
        double whiteKeyWidth = panelWidth / WhiteKeyCount;
        return pitch.IsAccidental
            ? whiteKeyWidth * BlackKeyWidthRatio
            : whiteKeyWidth;
    }

    public static bool IsInRange(NotePitch pitch)
    {
        return pitch.Value >= PianoRange.FirstPitch && pitch.Value <= PianoRange.LastPitch;
    }

    private static FrozenDictionary<NotePitch, int> BuildWhiteIndexByPitch()
    {
        Dictionary<NotePitch, int> map = [];
        int index = 0;
        for (ushort p = PianoRange.FirstPitch; p <= PianoRange.LastPitch; p++)
        {
            NotePitch pitch = new(p);
            if (!pitch.IsAccidental)
            {
                map[pitch] = index++;
            }
        }
        return map.ToFrozenDictionary();
    }
}
