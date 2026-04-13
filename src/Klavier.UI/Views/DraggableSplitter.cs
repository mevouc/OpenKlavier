using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;

namespace Klavier.UI.Views;

public class DraggableSplitter
{
    private static readonly SolidColorBrush _DefaultBrush = new(KlavierTheme.Divider);
    private static readonly SolidColorBrush _HoverBrush = new(KlavierTheme.Accent);

    private readonly Border _leftLine;
    private readonly Border _rightLine;
    private readonly TextBlock _gripDots;

    public GridSplitter HitArea { get; }
    public Grid Visual { get; }

    public DraggableSplitter(double height)
    {
        _leftLine = new Border
        {
            Height = 1,
            Background = _DefaultBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _rightLine = new Border
        {
            Height = 1,
            Background = _DefaultBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _gripDots = new TextBlock
        {
            Text = "● ● ●",
            Foreground = _DefaultBrush,
            FontSize = 6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(4, 0),
        };

        Visual = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
            IsHitTestVisible = false,
            IsVisible = false,
        };
        Grid.SetColumn(_leftLine, 0);
        Grid.SetColumn(_gripDots, 1);
        Grid.SetColumn(_rightLine, 2);
        Visual.Children.Add(_leftLine);
        Visual.Children.Add(_gripDots);
        Visual.Children.Add(_rightLine);

        HitArea = new GridSplitter
        {
            Height = height,
            MinHeight = height,
            MaxHeight = height,
            Background = Brushes.Transparent,
            IsVisible = false,
        };
        HitArea.PointerEntered += (_, _) => SetHoverState(true);
        HitArea.PointerExited += (_, _) => SetHoverState(false);
    }

    public bool IsVisible
    {
        get => HitArea.IsVisible;
        set
        {
            HitArea.IsVisible = value;
            Visual.IsVisible = value;
        }
    }

    private void SetHoverState(bool hover)
    {
        SolidColorBrush brush = hover ? _HoverBrush : _DefaultBrush;
        _leftLine.Background = brush;
        _rightLine.Background = brush;
        _gripDots.Foreground = brush;
    }
}
