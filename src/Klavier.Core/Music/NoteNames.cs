using Klavier.Config;
using Klavier.Core.Primitives;

namespace Klavier.Core.Music;

public static class NoteNames
{
    private static readonly string[] _ScientificNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static readonly string[] _SolfegeNames = ["Do", "Do#", "Re", "Re#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si"];

    private static readonly char[] _SubscriptDigits = ['₀', '₁', '₂', '₃', '₄', '₅', '₆', '₇', '₈', '₉'];

    public static string GetNoteName(NotePitch pitch, NoteNameStyle style)
    {
        return style switch
        {
            NoteNameStyle.Scientific => FormatScientific(pitch),
            NoteNameStyle.Solfege => FormatSolfege(pitch),
            NoteNameStyle.Helmholtz => FormatHelmholtz(pitch),
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
        };
    }

    private static string FormatScientific(NotePitch pitch)
    {
        int noteIndex = pitch.NoteIndex;
        int spnOctave = pitch.SpnOctave;

        return $"{_ScientificNames[noteIndex]}{spnOctave}";
    }

    private static string FormatSolfege(NotePitch pitch)
    {
        int noteIndex = pitch.NoteIndex;
        int solfegeOctave = pitch.SpnOctave - 1;

        return $"{_SolfegeNames[noteIndex]}{FormatSubscriptNumber(solfegeOctave)}";
    }

    private static string FormatSubscriptNumber(int number)
    {
        if (number >= 0 && number <= 9)
        {
            return _SubscriptDigits[number].ToString();
        }

        // Handle negative and multi-digit numbers
        string digits = number.ToString();
        char[] subscript = new char[digits.Length];

        for (int i = 0; i < digits.Length; i++)
        {
            subscript[i] = digits[i] == '-'
                ? '₋'
                : _SubscriptDigits[digits[i] - '0'];
        }

        return new string(subscript);
    }

    private static string FormatHelmholtz(NotePitch pitch)
    {
        int noteIndex = pitch.NoteIndex;
        int spnOctave = pitch.SpnOctave;

        string noteName = _ScientificNames[noteIndex];

        if (spnOctave <= 2)
        {
            // Uppercase, with subscript commas for octaves below Great octave (SPN 2)
            int commaCount = 2 - spnOctave;

            return commaCount > 0
                ? $"{noteName}{new string(',', commaCount)}"
                : noteName;
        }

        // Lowercase, with primes for octaves above Small octave (SPN 3)
        string lowerName = noteName.Length == 1
            ? noteName.ToLowerInvariant()
            : $"{char.ToLowerInvariant(noteName[0])}{noteName[1..]}";

        int primeCount = spnOctave - 3;

        return primeCount > 0
            ? $"{lowerName}{new string('′', primeCount)}"
            : lowerName;
    }
}
