using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;

namespace Klavier.UI.Views.Player;

public class ProgressBarView : Grid
{
    private const double _Height = 2;

    private readonly PlayerViewModel _viewModel;
    private readonly Rectangle _fill;

    public ProgressBarView(PlayerViewModel viewModel)
    {
        _viewModel = viewModel;

        Height = _Height;
        Background = new SolidColorBrush(ThemePaletteProvider.Divider);

        _fill = new Rectangle
        {
            Fill = new SolidColorBrush(ThemePaletteProvider.Accent),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0,
        };
        Children.Add(_fill);

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PlayerViewModel.Position) or nameof(PlayerViewModel.Duration))
            {
                UpdateFillWidth();
            }
        };
    }

    private void UpdateFillWidth()
    {
        TimeSpan duration = _viewModel.Duration;
        if (duration <= TimeSpan.Zero)
        {
            _fill.Width = 0;
            return;
        }
        double progress = Math.Clamp(_viewModel.Position.TotalSeconds / duration.TotalSeconds, 0, 1);
        _fill.Width = Bounds.Width * progress;
    }
}
