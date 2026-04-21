using Avalonia.Input;

namespace Klavier.UI.Input.Mapping;

public static class KeyModifierOptions
{
    public static readonly IReadOnlyList<(KeyModifiers Modifier, string Label)> All =
    [
        (KeyModifiers.Shift, "Shift"),
        (KeyModifiers.Control, "Ctrl"),
        (KeyModifiers.Alt, "Alt"),
    ];

    public static IReadOnlyList<string> AllLabels { get; } = [.. All.Select(o => o.Label)];

    public static string LabelOf(KeyModifiers modifier)
    {
        foreach ((KeyModifiers m, string label) in All)
        {
            if (m == modifier)
            {
                return label;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(modifier), modifier, "Unsupported key modifier.");
    }

    public static KeyModifiers? ParseLabel(string? label)
    {
        if (label is null)
        {
            return null;
        }

        foreach ((KeyModifiers modifier, string l) in All)
        {
            if (l == label)
            {
                return modifier;
            }
        }
        return null;
    }
}
