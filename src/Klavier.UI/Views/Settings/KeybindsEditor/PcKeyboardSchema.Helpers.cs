using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config.Schema;
using Klavier.Core.Music;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

public partial class PcKeyboardSchema
{
    private static readonly SolidColorBrush _BoundKeyBorderBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _UnboundKeyBorderBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _ActiveBorderBrush = new(UserPalette.Accent);
    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _HomeRowMarkerBrush = new(ThemePaletteProvider.NeutralSurface);

    private const double _KeyWidth = 46;
    private const double _KeyHeight = _KeyWidth;
    private const double _ModifierKeyWidth = 58;
    private const double _KeyLabelFontSize = 10;
    private const double _NoteLabelFontSize = 8;
    private const double _HomeRowMarkerFontSize = 18;
    private const double _HomeRowMarkerBottomMargin = 4;

    private static Control BuildKeyBlock(
        PhysicalKey key,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle)
    {
        bool isBound = whiteBindings.ContainsKey(key) || blackBindings.ContainsKey(key);
        IBrush border = isBound ? _BoundKeyBorderBrush : _UnboundKeyBorderBrush;
        Control content = BuildKeyContent(key, whiteBindings, blackBindings, noteNameStyle);
        return IsLayoutDependent(key) ? BuildDashedKeyFrame(content, border) : BuildSolidKeyFrame(content, border);
    }

    private static Control BuildKeyContent(
        PhysicalKey key,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle)
    {
        StackPanel labels = BuildKeyLabels(key, whiteBindings, blackBindings, noteNameStyle);
        if (!HasHomeRowMarker(key))
        {
            return labels;
        }

        Grid layout = new();
        layout.Children.Add(labels);
        layout.Children.Add(BuildHomeRowMarker());
        return layout;
    }

    private static bool HasHomeRowMarker(PhysicalKey key) =>
        key is PhysicalKey.F or PhysicalKey.J;

    private static TextBlock BuildHomeRowMarker() => new()
    {
        Text = "_",
        FontSize = _HomeRowMarkerFontSize,
        FontWeight = FontWeight.Bold,
        Foreground = _HomeRowMarkerBrush,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(0, 0, 0, _HomeRowMarkerBottomMargin),
    };

    private static StackPanel BuildKeyLabels(
        PhysicalKey key,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle)
    {
        StackPanel content = new()
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // Blacks are derived from whites + layout modifier, so we only render white entries.
        // Fall back to the black entry only for the key label (e.g. when a user has a black-only binding
        // via a legacy file or via a key that is only mapped as modifier+key).
        bool hasWhite = whiteBindings.TryGetValue(key, out KeyMappingEntry whiteEntry);
        bool hasBlack = blackBindings.TryGetValue(key, out KeyMappingEntry blackEntry);

        string? keyLabel = hasWhite ? whiteEntry.Label : (hasBlack ? blackEntry.Label : null);
        if (keyLabel is not null)
        {
            content.Children.Add(BuildKeyLabelText(keyLabel));
        }

        if (hasWhite)
        {
            content.Children.Add(BuildNoteLabelText(NoteNames.GetNoteName(whiteEntry.Pitch, noteNameStyle)));
        }

        return content;
    }

    private static Border BuildSolidKeyFrame(Control content, IBrush border) => new()
    {
        BorderBrush = border,
        BorderThickness = new Thickness(Constants.BorderThickness),
        CornerRadius = new CornerRadius(Constants.CornerRadius),
        Width = _KeyWidth,
        Height = _KeyHeight,
        Child = content,
    };

    private static Panel BuildDashedKeyFrame(Control content, IBrush border) => new()
    {
        Width = _KeyWidth,
        Height = _KeyHeight,
        Children =
        {
            new Rectangle
            {
                Stroke = border,
                StrokeThickness = Constants.BorderThickness,
                StrokeDashArray = [2, 2],
                RadiusX = Constants.CornerRadius,
                RadiusY = Constants.CornerRadius,
            },
            content,
        },
    };

    private static bool IsLayoutDependent(PhysicalKey key) =>
        key is PhysicalKey.Backslash or PhysicalKey.IntlBackslash;

    private static TextBlock BuildKeyLabelText(string text) => new()
    {
        Text = text,
        FontSize = _KeyLabelFontSize,
        FontWeight = FontWeight.Bold,
        Foreground = _TextBrush,
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    private static TextBlock BuildNoteLabelText(string text) => new()
    {
        Text = text,
        FontSize = _NoteLabelFontSize,
        Foreground = _TextBrush,
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    private static Border BuildModifierBlock(string label, bool isActive) => new()
    {
        BorderBrush = isActive ? _ActiveBorderBrush : _BoundKeyBorderBrush,
        BorderThickness = new Thickness(Constants.BorderThickness),
        CornerRadius = new CornerRadius(Constants.CornerRadius),
        Width = _ModifierKeyWidth,
        Height = _KeyHeight,
        Child = new TextBlock
        {
            Text = label,
            FontSize = _KeyLabelFontSize,
            FontWeight = isActive ? FontWeight.Bold : FontWeight.Normal,
            Foreground = _TextBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
