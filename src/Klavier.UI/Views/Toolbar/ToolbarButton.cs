using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Toolbar;

public class ToolbarButton : Border
{
    private static readonly SolidColorBrush _PanelBrush = new(ThemePaletteProvider.PanelBackground);
    private static readonly SolidColorBrush _DefaultBorderBrush = new(ThemePaletteProvider.PanelBackground);
    private static readonly SolidColorBrush _ActiveBorderBrush = new(ThemePaletteProvider.Accent);
    private static readonly SolidColorBrush _DefaultTextBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _ActiveTextBrush = new(ThemePaletteProvider.Accent);

    private readonly TextBlock _label;
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            BorderBrush = _isActive ? _ActiveBorderBrush : _DefaultBorderBrush;
            _label.Foreground = _isActive ? _ActiveTextBrush : _DefaultTextBrush;
        }
    }

    public ToolbarButton(string text)
    {
        _label = new TextBlock
        {
            Text = text,
            Foreground = _DefaultTextBrush,
            FontSize = Constants.KeyLabelsFontSize,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Background = _PanelBrush;
        BorderBrush = _DefaultBorderBrush;
        BorderThickness = new Thickness(2);
        CornerRadius = new CornerRadius(Constants.CornerRadius);
        Padding = new Thickness(12, 4);
        Cursor = new Cursor(StandardCursorType.Hand);
        Child = _label;
    }
}
