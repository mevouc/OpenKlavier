using Avalonia;
using Avalonia.Controls;

namespace Klavier.UI.Views;

/// <summary>
/// Manages a collapsible row + splitter + content as one unit in a Window's outer Grid.
/// On open, sets the row to a default height, shows the splitter, grows the window by the row delta,
/// and (optionally) shifts the window's top edge upward so the visible portion stays anchored.
/// </summary>
public class CollapsibleSection
{
    private readonly Control _content;
    private readonly DraggableSplitter _splitter;
    private readonly RowDefinition _row;
    private readonly Window _window;
    private readonly double _defaultHeight;
    private readonly double _minHeight;
    private readonly int _splitterLayoutHeight;
    private readonly bool _growUpward;
    private readonly Func<double>? _measureContentHeight;

    private bool _isOpen;
    private double _myMinHeightContribution;

    /// <param name="splitterLayoutHeight">DIPs the splitter consumes in layout. 0 if straddling the boundary; otherwise the splitter row's height.</param>
    /// <param name="growUpward">If true, the window's top edge moves up on open (bottom anchored). If false, the window grows downward (top anchored).</param>
    /// <param name="measureContentHeight">Optional. Returned value clamps the open height (Math.Min) and sets the row's MaxHeight, so the section can't be dragged larger than its natural content.</param>
    public CollapsibleSection(
        Control content,
        DraggableSplitter splitter,
        RowDefinition row,
        Window window,
        double defaultHeight,
        double minHeight,
        int splitterLayoutHeight,
        bool growUpward,
        Func<double>? measureContentHeight = null)
    {
        _content = content;
        _splitter = splitter;
        _row = row;
        _window = window;
        _defaultHeight = defaultHeight;
        _minHeight = minHeight;
        _splitterLayoutHeight = splitterLayoutHeight;
        _growUpward = growUpward;
        _measureContentHeight = measureContentHeight;

        // Initialize closed state.
        _content.IsVisible = false;
        _row.Height = new GridLength(0);
        _row.MinHeight = 0;

        // Avalonia's Grid does NOT compress fixed-pixel rows when star rows demand their MinHeight - the
        // deficit just clips at the window's bottom edge. So _window.MinHeight must reflect the row's actual
        // current Height (not just the row's MinHeight), so the user can never shrink the window past where
        // the section's content fits. Splitter drags mutate _row.Height, which fires this observer.
        _row.PropertyChanged += (_, e) =>
        {
            if (e.Property == RowDefinition.HeightProperty)
            {
                UpdateMinHeightContribution();
            }
        };
    }

    public bool IsOpen => _isOpen;

    public void SetOpen(bool open)
    {
        if (_isOpen == open)
        {
            return;
        }
        _isOpen = open;

        _content.IsVisible = open;
        _splitter.IsVisible = open;

        if (open)
        {
            double openHeight = _defaultHeight;
            double maxHeight = double.PositiveInfinity;
            if (_measureContentHeight is not null)
            {
                double contentHeight = _measureContentHeight();
                openHeight = Math.Min(openHeight, contentHeight);
                maxHeight = contentHeight;
            }
            _row.Height = new GridLength(openHeight);
            _row.MinHeight = _minHeight;
            _row.MaxHeight = maxHeight;

            AdjustWindowSize(openHeight + _splitterLayoutHeight, growing: true);
        }
        else
        {
            double previousHeight = _row.Height.Value;
            _row.Height = new GridLength(0);
            _row.MinHeight = 0;
            _row.MaxHeight = double.PositiveInfinity;

            AdjustWindowSize(previousHeight + _splitterLayoutHeight, growing: false);
        }
    }

    private void UpdateMinHeightContribution()
    {
        double newContribution = _isOpen ? _row.Height.Value + _splitterLayoutHeight : 0;
        double delta = newContribution - _myMinHeightContribution;
        if (delta == 0)
        {
            return;
        }
        _window.MinHeight += delta;
        _myMinHeightContribution = newContribution;
    }

    private void AdjustWindowSize(double dipDelta, bool growing)
    {
        if (_growUpward)
        {
            // Window's bottom edge stays anchored; top edge moves.
            // Position uses physical pixels (PixelPoint) while Height uses DIPs - convert via DesktopScaling.
            int positionDelta = (int)Math.Round(dipDelta * _window.DesktopScaling);
            if (growing)
            {
                _window.Position = new PixelPoint(_window.Position.X, _window.Position.Y - positionDelta);
                _window.Height += dipDelta;
            }
            else
            {
                _window.Height -= dipDelta;
                _window.Position = new PixelPoint(_window.Position.X, _window.Position.Y + positionDelta);
            }
        }
        else
        {
            // Grow downward: window's top edge stays anchored; bottom edge moves.
            _window.Height += growing ? dipDelta : -dipDelta;
        }
    }
}
