using Avalonia;
using Avalonia.Controls;
using Klavier.UI.ViewModels;

namespace Klavier.UI.Views.Piano;

public class PianoView : Panel
{
    private const int _MinHeight = 100;
    private const double _BlackKeyHeightRatio = 0.55;
    private const double _SustainBarHeight = 36;
    private const double _SustainBarWidthRatio = 0.35;
    private const double _SustainBarGap = 4;
    private const double _SustainBarBottomMargin = 6;

    private readonly List<PianoKeyControl> _whiteKeys = [];
    private readonly List<PianoKeyControl> _blackKeys = [];
    private readonly SustainBarControl? _sustainBar;

    public IReadOnlyList<PianoKeyControl> WhiteKeys => _whiteKeys;
    public IReadOnlyList<PianoKeyControl> BlackKeys => _blackKeys;

    public PianoView(IReadOnlyList<PianoKeyViewModel> keys, SustainBarControl? sustainBar = null)
    {
        _sustainBar = sustainBar;

        MinHeight = _MinHeight;

        // Separate white and black keys, maintaining order
        foreach (PianoKeyViewModel keyViewModel in keys)
        {
            PianoKeyControl control = new(keyViewModel);

            if (keyViewModel.IsBlack)
            {
                _blackKeys.Add(control);
            }
            else
            {
                _whiteKeys.Add(control);
            }
        }

        // Add white keys first (behind), then black keys (in front) for z-order
        foreach (PianoKeyControl whiteKey in _whiteKeys)
        {
            Children.Add(whiteKey);
        }

        foreach (PianoKeyControl blackKey in _blackKeys)
        {
            Children.Add(blackKey);
        }

        if (_sustainBar is not null)
        {
            Children.Add(_sustainBar);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_whiteKeys.Count == 0)
        {
            return finalSize;
        }

        double whiteKeyHeight = _sustainBar is null
            ? finalSize.Height
            : Math.Max(0, finalSize.Height - _SustainBarHeight - _SustainBarGap - _SustainBarBottomMargin);
        double blackKeyHeight = whiteKeyHeight * _BlackKeyHeightRatio;

        foreach (PianoKeyControl whiteKey in _whiteKeys)
        {
            double x = PianoKeyGeometry.GetColumnLeftX(whiteKey.Pitch, finalSize.Width);
            double width = PianoKeyGeometry.GetColumnWidth(whiteKey.Pitch, finalSize.Width);
            whiteKey.Arrange(new Rect(x, 0, width, whiteKeyHeight));
        }

        foreach (PianoKeyControl blackKey in _blackKeys)
        {
            double x = PianoKeyGeometry.GetColumnLeftX(blackKey.Pitch, finalSize.Width);
            double width = PianoKeyGeometry.GetColumnWidth(blackKey.Pitch, finalSize.Width);
            blackKey.Arrange(new Rect(x, 0, width, blackKeyHeight));
        }

        // Arrange sustain bar below keys, centered like a space bar
        if (_sustainBar is not null)
        {
            double sustainBarWidth = finalSize.Width * _SustainBarWidthRatio;
            double sustainX = (finalSize.Width - sustainBarWidth) / 2;
            double sustainY = whiteKeyHeight + _SustainBarGap;
            _sustainBar.Arrange(new Rect(sustainX, sustainY, sustainBarWidth, _SustainBarHeight));
        }

        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (Control child in Children)
        {
            child.Measure(availableSize);
        }

        return availableSize;
    }
}
