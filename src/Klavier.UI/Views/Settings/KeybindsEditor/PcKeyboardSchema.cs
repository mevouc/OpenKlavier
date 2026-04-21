using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Klavier.Config;
using Klavier.UI.Input.Mapping;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

public partial class PcKeyboardSchema : StackPanel
{
    private const double _RowSpacing = 2;
    private const double _KeySpacing = 2;
    private const double _KeyUnit = _KeyWidth + _KeySpacing;

    // Typewriter-style per-row x-axis shift. Row 4's first key (Z) aligns with row 1's "2".
    private static readonly (PhysicalKey[] Keys, double Offset)[] _KeyRows =
    [
        (
            [
                PhysicalKey.Backquote,
                PhysicalKey.Digit1, PhysicalKey.Digit2, PhysicalKey.Digit3, PhysicalKey.Digit4,
                PhysicalKey.Digit5, PhysicalKey.Digit6, PhysicalKey.Digit7, PhysicalKey.Digit8,
                PhysicalKey.Digit9, PhysicalKey.Digit0,
                PhysicalKey.Minus, PhysicalKey.Equal,
            ],
            0
        ),
        (
            [
                PhysicalKey.Q, PhysicalKey.W, PhysicalKey.E, PhysicalKey.R, PhysicalKey.T,
                PhysicalKey.Y, PhysicalKey.U, PhysicalKey.I, PhysicalKey.O, PhysicalKey.P,
                PhysicalKey.BracketLeft, PhysicalKey.BracketRight,
                PhysicalKey.Backslash, // US ANSI position of `|`
            ],
            _KeyUnit * 1.33
        ),
        (
            [
                PhysicalKey.A, PhysicalKey.S, PhysicalKey.D, PhysicalKey.F, PhysicalKey.G,
                PhysicalKey.H, PhysicalKey.J, PhysicalKey.K, PhysicalKey.L,
                PhysicalKey.Semicolon, PhysicalKey.Quote,
                PhysicalKey.Backslash, // UK ISO position of `#~` (same PhysicalKey as US `\|` on row 2)
            ],
            _KeyUnit * 1.67
        ),
        (
            [
                PhysicalKey.IntlBackslash, // UK/ISO position of `|`
                PhysicalKey.Z, PhysicalKey.X, PhysicalKey.C, PhysicalKey.V, PhysicalKey.B,
                PhysicalKey.N, PhysicalKey.M,
                PhysicalKey.Comma, PhysicalKey.Period, PhysicalKey.Slash,
            ],
            _KeyUnit * 2.0
        ),
    ];

    public PcKeyboardSchema(
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        KeyModifiers activeModifier,
        NoteNameStyle noteNameStyle)
    {
        Orientation = Orientation.Vertical;
        Spacing = _RowSpacing;
        HorizontalAlignment = HorizontalAlignment.Center;

        foreach ((PhysicalKey[] keys, double offset) in _KeyRows)
        {
            Children.Add(BuildKeyRow(keys, offset, whiteBindings, blackBindings, noteNameStyle));
        }
        Children.Add(BuildModifierRow(activeModifier));
    }

    private static StackPanel BuildKeyRow(
        IReadOnlyList<PhysicalKey> keys,
        double leftOffset,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle)
    {
        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = _KeySpacing,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(leftOffset, 0, 0, 0),
        };

        foreach (PhysicalKey key in keys)
        {
            row.Children.Add(BuildKeyBlock(key, whiteBindings, blackBindings, noteNameStyle));
        }

        return row;
    }

    private static StackPanel BuildModifierRow(KeyModifiers activeModifier)
    {
        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = _KeySpacing,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        foreach ((KeyModifiers modifier, string label) in KeyModifierOptions.All)
        {
            row.Children.Add(BuildModifierBlock(label, modifier == activeModifier));
        }

        return row;
    }
}
