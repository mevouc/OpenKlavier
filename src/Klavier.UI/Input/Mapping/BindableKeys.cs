using System.Collections.Frozen;
using Avalonia.Input;

namespace Klavier.UI.Input.Mapping;

/// <summary>
/// The set of <see cref="PhysicalKey"/> values the app accepts for binding,
/// organized by PC-keyboard row. Source of truth for both validation and rendering.
/// </summary>
public static class BindableKeys
{
    public static readonly IReadOnlyList<IReadOnlyList<PhysicalKey>> Rows =
    [
        // Digit row
        [
            PhysicalKey.Backquote,
            PhysicalKey.Digit1, PhysicalKey.Digit2, PhysicalKey.Digit3, PhysicalKey.Digit4,
            PhysicalKey.Digit5, PhysicalKey.Digit6, PhysicalKey.Digit7, PhysicalKey.Digit8,
            PhysicalKey.Digit9, PhysicalKey.Digit0,
            PhysicalKey.Minus, PhysicalKey.Equal,
        ],
        // Top letter row (includes US ANSI `|` slot at end; same PhysicalKey as UK `#~` on row 2)
        [
            PhysicalKey.Q, PhysicalKey.W, PhysicalKey.E, PhysicalKey.R, PhysicalKey.T,
            PhysicalKey.Y, PhysicalKey.U, PhysicalKey.I, PhysicalKey.O, PhysicalKey.P,
            PhysicalKey.BracketLeft, PhysicalKey.BracketRight,
            PhysicalKey.Backslash,
        ],
        // Home letter row (ends with UK ISO `#~` — same PhysicalKey.Backslash, appears twice on purpose)
        [
            PhysicalKey.A, PhysicalKey.S, PhysicalKey.D, PhysicalKey.F, PhysicalKey.G,
            PhysicalKey.H, PhysicalKey.J, PhysicalKey.K, PhysicalKey.L,
            PhysicalKey.Semicolon, PhysicalKey.Quote,
            PhysicalKey.Backslash,
        ],
        // Bottom letter row (starts with UK/ISO-only `|\` key)
        [
            PhysicalKey.IntlBackslash,
            PhysicalKey.Z, PhysicalKey.X, PhysicalKey.C, PhysicalKey.V, PhysicalKey.B,
            PhysicalKey.N, PhysicalKey.M,
            PhysicalKey.Comma, PhysicalKey.Period, PhysicalKey.Slash,
        ],
    ];

    public static readonly FrozenSet<PhysicalKey> All = Rows.SelectMany(r => r).ToFrozenSet();
}
