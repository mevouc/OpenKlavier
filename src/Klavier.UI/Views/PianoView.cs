using Avalonia;
using Avalonia.Controls;
using Klavier.UI.ViewModels;

namespace Klavier.UI.Views;

public class PianoView : Panel
{
    private const double _BlackKeyWidthRatio = 0.68;
    private const double _BlackKeyHeightRatio = 0.55;

    private readonly List<PianoKeyControl> _whiteKeys = [];
    private readonly List<PianoKeyControl> _blackKeys = [];

    // Maps each white key's index in _whiteKeys to whether it has a black key after it
    private readonly List<bool> _whiteKeyHasSharp = [];

    public PianoView(PianoViewModel viewModel)
    {
        // Separate white and black keys, maintaining order
        foreach (PianoKeyViewModel keyViewModel in viewModel.Keys)
        {
            PianoKeyControl control = new(keyViewModel);

            if (keyViewModel.IsBlack)
            {
                _blackKeys.Add(control);
            }
            else
            {
                _whiteKeys.Add(control);

                // C, D, F, G, A have sharps (note indices 0, 2, 5, 7, 9)
                int noteIndex = keyViewModel.Pitch.Value % 12;
                _whiteKeyHasSharp.Add(noteIndex is 0 or 2 or 5 or 7 or 9);
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
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int whiteKeyCount = _whiteKeys.Count;

        if (whiteKeyCount == 0)
        {
            return finalSize;
        }

        double whiteKeyWidth = finalSize.Width / whiteKeyCount;
        double whiteKeyHeight = finalSize.Height;
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
