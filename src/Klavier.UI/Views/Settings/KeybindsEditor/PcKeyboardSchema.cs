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

    // Typewriter-style x-axis shift per row (indexed into BindableKeys.Rows).
    // Row 4's first key (Z) aligns with row 1's "2".
    private static readonly double[] _RowOffsets =
    [
        0,
        _KeyUnit * 1.33,
        _KeyUnit * 1.67,
        _KeyUnit * 2.0,
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

        for (int i = 0; i < BindableKeys.Rows.Count; i++)
        {
            Children.Add(BuildKeyRow(BindableKeys.Rows[i], _RowOffsets[i], whiteBindings, blackBindings, noteNameStyle));
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

        foreach ((KeyModifiers modifier, string label, string _) in KeyModifierOptions.All)
        {
            row.Children.Add(BuildModifierBlock(label, modifier == activeModifier));
        }

        return row;
    }
}
