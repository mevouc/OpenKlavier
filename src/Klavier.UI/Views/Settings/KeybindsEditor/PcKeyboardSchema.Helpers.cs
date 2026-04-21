using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Music;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

public partial class PcKeyboardSchema
{
    private static readonly SolidColorBrush _KeyBorderBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _ActiveBorderBrush = new(UserPalette.Accent);
    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);

    private const double _KeyWidth = 46;
    private const double _KeyHeight = _KeyWidth;
    private const double _ModifierKeyWidth = 52;
    private const double _KeyLabelFontSize = 10;
    private const double _NoteLabelFontSize = 8;

    private static Control BuildKeyBlock(
        PhysicalKey key,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle)
    {
        StackPanel content = BuildKeyContent(key, whiteBindings, blackBindings, noteNameStyle);
        return IsLayoutDependent(key) ? BuildDashedKeyFrame(content) : BuildSolidKeyFrame(content);
    }

    private static StackPanel BuildKeyContent(
        PhysicalKey key,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> whiteBindings,
        IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> blackBindings,
        NoteNameStyle noteNameStyle)
    {
        bool hasWhite = whiteBindings.TryGetValue(key, out KeyMappingEntry whiteEntry);
        bool hasBlack = blackBindings.TryGetValue(key, out KeyMappingEntry blackEntry);

        StackPanel content = new()
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        string? keyLabel = hasWhite ? whiteEntry.Label : (hasBlack ? blackEntry.Label : null);
        if (keyLabel is not null)
        {
            content.Children.Add(BuildKeyLabelText(keyLabel));
        }

        if (hasWhite)
        {
            content.Children.Add(BuildNoteLabelText(NoteNames.GetNoteName(whiteEntry.Pitch, noteNameStyle)));
        }

        if (hasBlack)
        {
            content.Children.Add(BuildNoteLabelText("/ " + NoteNames.GetNoteName(blackEntry.Pitch, noteNameStyle)));
        }

        return content;
    }

    private static Border BuildSolidKeyFrame(Control content) => new()
    {
        BorderBrush = _KeyBorderBrush,
        BorderThickness = new Thickness(Constants.BorderThickness),
        CornerRadius = new CornerRadius(Constants.CornerRadius),
        Width = _KeyWidth,
        Height = _KeyHeight,
        Child = content,
    };

    private static Panel BuildDashedKeyFrame(Control content) => new()
    {
        Width = _KeyWidth,
        Height = _KeyHeight,
        Children =
        {
            new Rectangle
            {
                Stroke = _KeyBorderBrush,
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
        BorderBrush = isActive ? _ActiveBorderBrush : _KeyBorderBrush,
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
