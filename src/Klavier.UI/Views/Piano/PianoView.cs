using Avalonia;
using Avalonia.Controls;
using Klavier.Core.Primitives;
using Klavier.UI.ViewModels;

namespace Klavier.UI.Views.Piano;

public class PianoView : Panel
{
    private const int _MinHeight = 100;
    private const double _BlackKeyWidthRatio = 0.68;
    private const double _BlackKeyHeightRatio = 0.55;
    private const double _SustainBarHeight = 36;
    private const double _SustainBarWidthRatio = 0.35;
    private const double _SustainBarGap = 4;
    private const double _SustainBarBottomMargin = 6;

    private readonly List<PianoKeyControl> _whiteKeys = [];
    private readonly List<PianoKeyControl> _blackKeys = [];
    private readonly SustainBarControl? _sustainBar;

    // Maps each white key's index in _whiteKeys to whether it has a black key after it
    private readonly List<bool> _whiteKeyHasSharp = [];

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

                // Check if the next semitone is an accidental (sharp)
                bool hasSharp = keyViewModel.Pitch.Value < NotePitch.MaxValue
                    && new NotePitch((ushort)(keyViewModel.Pitch.Value + 1)).IsAccidental;
                _whiteKeyHasSharp.Add(hasSharp);
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
        int whiteKeyCount = _whiteKeys.Count;

        if (whiteKeyCount == 0)
        {
            return finalSize;
        }

        double whiteKeyWidth = finalSize.Width / whiteKeyCount;
        double whiteKeyHeight = _sustainBar is null
            ? finalSize.Height
            : Math.Max(0, finalSize.Height - _SustainBarHeight - _SustainBarGap - _SustainBarBottomMargin);
        double blackKeyWidth = whiteKeyWidth * _BlackKeyWidthRatio;
        double blackKeyHeight = whiteKeyHeight * _BlackKeyHeightRatio;

        // Arrange white keys
        for (int i = 0; i < whiteKeyCount; i++)
        {
            double x = i * whiteKeyWidth;

            _whiteKeys[i].Arrange(new Rect(x, 0, whiteKeyWidth, whiteKeyHeight));
        }

        // Arrange black keys at the boundary after white keys that have sharps
        int blackIndex = 0;

        for (int i = 0; i < whiteKeyCount && blackIndex < _blackKeys.Count; i++)
        {
            if (_whiteKeyHasSharp[i])
            {
                double x = ((i + 1) * whiteKeyWidth) - (blackKeyWidth / 2);

                _blackKeys[blackIndex].Arrange(new Rect(x, 0, blackKeyWidth, blackKeyHeight));
                blackIndex++;
            }
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
