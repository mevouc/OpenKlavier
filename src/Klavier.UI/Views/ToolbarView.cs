using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Core.Engine;
using Klavier.UI.Theme;

namespace Klavier.UI.Views;

public class ToolbarView : Border
{
    private static readonly SolidColorBrush _PanelBrush = new(KlavierTheme.PanelBackground);
    private static readonly SolidColorBrush _DefaultBorderBrush = new(KlavierTheme.PanelBackground);
    private static readonly SolidColorBrush _ActiveBorderBrush = new(KlavierTheme.Accent);
    private static readonly SolidColorBrush _DefaultTextBrush = new(KlavierTheme.TextPrimary);
    private static readonly SolidColorBrush _ActiveTextBrush = new(KlavierTheme.Accent);

    public ToolbarView(IPianoEngine pianoEngine)
    {
        Background = new SolidColorBrush(KlavierTheme.AppBackground);
        Padding = new Thickness(8, 4);

        TextBlock panicLabel = new()
        {
            Text = "Panic",
            Foreground = _DefaultTextBrush,
            FontSize = Constants.KeyLabelsFontSize,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Border panicButton = new()
        {
            Background = _PanelBrush,
            BorderBrush = _DefaultBorderBrush,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(Constants.CornerRadius),
            Padding = new Thickness(12, 4),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = panicLabel,
        };

        panicButton.PointerPressed += (_, e) =>
        {
            pianoEngine.AllNotesOff();
            panicButton.BorderBrush = _ActiveBorderBrush;
            panicLabel.Foreground = _ActiveTextBrush;
            e.Handled = true;
        };

        panicButton.PointerReleased += (_, e) =>
        {
            panicButton.BorderBrush = _DefaultBorderBrush;
            panicLabel.Foreground = _DefaultTextBrush;
            e.Handled = true;
        };

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { panicButton },
        };
    }
}
