using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Controls;

public class StyledComboBox : ComboBox
{
    private static readonly SolidColorBrush _ContrastedSurfaceBrush = new(ThemePaletteProvider.ContrastedSurface);
    private static readonly SolidColorBrush _NeutralSurfaceBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _HoverHighlightBrush = new(ThemePaletteProvider.HoverHighlight);

    // Inherit the Fluent ComboBox control template (Avalonia style matching is exact-type by default).
    protected override Type StyleKeyOverride => typeof(ComboBox);

    public StyledComboBox()
    {
        VerticalAlignment = VerticalAlignment.Center;
        MinWidth = 120;
        Focusable = false;
        Background = _ContrastedSurfaceBrush;
        BorderBrush = _NeutralSurfaceBrush;
        Resources["ComboBoxBorderBrushPointerOver"] = _HoverHighlightBrush;
    }
}
