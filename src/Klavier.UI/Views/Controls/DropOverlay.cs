using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;

namespace Klavier.UI.Views.Controls;

/// <summary>
/// Translucent backdrop with a dashed accent frame and a centered label, hidden by default.
/// IsHitTestVisible=false lets drag events pass through to the underlying drag handlers.
/// Observes <see cref="MainWindowViewModel"/> for visibility/label state.
/// </summary>
public class DropOverlay : Border
{
    private const double _LabelFontSize = 28;
    private const double _DashedFrameStrokeThickness = 4;

    private readonly TextBlock _label;

    public DropOverlay(MainWindowViewModel viewModel)
    {
        _label = new TextBlock
        {
            Text = viewModel.DropOverlayLabel,
            FontSize = _LabelFontSize,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(ThemePaletteProvider.Inverse),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Rectangle dashedFrame = new()
        {
            Stroke = new SolidColorBrush(ThemePaletteProvider.Inverse),
            StrokeThickness = _DashedFrameStrokeThickness,
            StrokeDashArray = [6, 4],
            Margin = new Thickness(2),
        };

        Background = new SolidColorBrush(ThemePaletteProvider.MediumContrasted) { Opacity = 0.7 };
        IsHitTestVisible = false;
        IsVisible = viewModel.IsDropOverlayVisible;
        Child = new Grid { Children = { dashedFrame, _label } };

        viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowViewModel.IsDropOverlayVisible):
                    IsVisible = viewModel.IsDropOverlayVisible;
                    break;
                case nameof(MainWindowViewModel.DropOverlayLabel):
                    _label.Text = viewModel.DropOverlayLabel;
                    break;
            }
        };
    }
}
