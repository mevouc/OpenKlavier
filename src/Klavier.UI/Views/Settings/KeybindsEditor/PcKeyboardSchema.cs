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
    private const double _CtrlAltRowOffset = 0;

    // Typewriter-style x-axis shift per row (indexed into BindableKeys.Rows).
    private static readonly double[] _RowOffsets =
    [
        0,
        _KeyUnit * 1.33,
        _KeyUnit * 1.67,
        0, // bottom letter row: prefixed with Shift, so starts at the left edge like on a real keyboard.
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

        int lastIndex = BindableKeys.Rows.Count - 1;
        for (int i = 0; i < BindableKeys.Rows.Count; i++)
        {
            Control? prefix = i == lastIndex
                ? BuildModifierBlock("Shift", activeModifier == KeyModifiers.Shift)
                : null;
            Children.Add(BuildKeyRow(BindableKeys.Rows[i], _RowOffsets[i], whiteBindings, blackBindings, noteNameStyle, prefix));
        }
        Children.Add(BuildControlAltRow(activeModifier));
    }

    private static StackPanel BuildKeyRow(
        IReadOnlyList<PhysicalKey> keys,
        double leftOffset,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle,
        Control? prefix = null)
    {
        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = _KeySpacing,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(leftOffset, 0, 0, 0),
        };

        if (prefix is not null)
        {
            row.Children.Add(prefix);
        }

        foreach (PhysicalKey key in keys)
        {
            row.Children.Add(BuildKeyBlock(key, whiteBindings, blackBindings, noteNameStyle));
        }

        return row;
    }

    private static StackPanel BuildControlAltRow(KeyModifiers activeModifier)
    {
        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = _KeySpacing,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(_CtrlAltRowOffset, 0, 0, 0),
        };

        row.Children.Add(BuildModifierBlock("Ctrl", activeModifier == KeyModifiers.Control));
        row.Children.Add(BuildModifierBlock("Alt", activeModifier == KeyModifiers.Alt));

        return row;
    }
}
