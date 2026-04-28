using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Klavier.Config.Schema;
using Klavier.Core.Primitives;
using Klavier.Midi;
using Klavier.UI.Theme;
using Klavier.UI.Threading;
using Klavier.UI.ViewModels;
using Klavier.UI.Views.Piano;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Player;

public class FallingNotesView : Control
{
    private static readonly SolidColorBrush _NotesColorBrush = new(ThemePaletteProvider.Accent);

    private readonly PlayerViewModel _viewModel;
    private readonly IOptionsMonitor<PlayerConfig> _playerConfig;

    // Cursor over score.Notes (sorted by Start): notes before this index are guaranteed to have ended.
    // Advances during Render and resets on score change or position rewind (stop / replay).
    private int _firstActiveIndex;
    private TimeSpan _lastRenderPosition;

    public FallingNotesView(PlayerViewModel viewModel, IOptionsMonitor<PlayerConfig> playerConfig)
    {
        _viewModel = viewModel;
        _playerConfig = playerConfig;

        ClipToBounds = true;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PlayerViewModel.Position) or nameof(PlayerViewModel.CurrentScore))
            {
                if (e.PropertyName == nameof(PlayerViewModel.CurrentScore))
                {
                    _firstActiveIndex = 0;
                    _lastRenderPosition = TimeSpan.Zero;
                }
                InvalidateVisual();
            }
        };
        _playerConfig.OnChangeOnUIThread(_ => InvalidateVisual());
    }

    public override void Render(DrawingContext context)
    {
        double panelWidth = Bounds.Width;
        double panelHeight = Bounds.Height;
        if (panelWidth <= 0 || panelHeight <= 0)
        {
            return;
        }

        DrawColumnHints(context, panelWidth, panelHeight);

        MidiScore? score = _viewModel.CurrentScore;
        if (score is null)
        {
            return;
        }

        double lookaheadSeconds = _playerConfig.CurrentValue.LookaheadSeconds;
        if (lookaheadSeconds <= 0)
        {
            return;
        }

        TimeSpan position = _viewModel.Position;
        if (position < _lastRenderPosition)
        {
            // Position rewound (stop, replay, finished->reset): the cursor invariant no longer holds.
            _firstActiveIndex = 0;
        }
        _lastRenderPosition = position;

        IReadOnlyList<MidiNote> notes = score.Notes;

        // Advance the cursor past notes that have fully ended. Sorted-by-Start means once a note's end
        // has passed, all earlier notes (smaller Start) with shorter-or-equal Duration have also ended;
        // longer notes that started earlier are caught by the secondsUntilEnd <= 0 check below.
        while (_firstActiveIndex < notes.Count
            && notes[_firstActiveIndex].Start + notes[_firstActiveIndex].Duration <= position)
        {
            _firstActiveIndex++;
        }

        for (int i = _firstActiveIndex; i < notes.Count; i++)
        {
            MidiNote note = notes[i];
            double secondsUntilStart = (note.Start - position).TotalSeconds;
            if (secondsUntilStart >= lookaheadSeconds)
            {
                // Sorted by Start: every later note is also too far in the future.
                break;
            }

            double secondsUntilEnd = (note.Start + note.Duration - position).TotalSeconds;
            if (secondsUntilEnd <= 0 || !PianoKeyGeometry.IsInRange(note.Pitch))
            {
                continue;
            }

            // Linear map: secondsUntil=0 → y=panelHeight (piano line); secondsUntil=lookaheadSeconds → y=0 (top).
            double barBottomY = panelHeight * (1 - (secondsUntilStart / lookaheadSeconds));
            double barTopY = panelHeight * (1 - (secondsUntilEnd / lookaheadSeconds));

            double x = PianoKeyGeometry.GetColumnLeftX(note.Pitch, panelWidth);
            double width = PianoKeyGeometry.GetColumnWidth(note.Pitch, panelWidth);

            double height = barBottomY - barTopY;

            Rect rect = new(x, barTopY, width, height);
            context.DrawRectangle(_NotesColorBrush, null, rect, Constants.CornerRadius, Constants.CornerRadius);
        }
    }

    private static void DrawColumnHints(DrawingContext context, double panelWidth, double panelHeight)
    {
        IBrush dividerBrush = new SolidColorBrush(ThemePaletteProvider.Divider);
        Pen solidPen = new(dividerBrush, 1.0);
        Pen dashPen = new(dividerBrush, 1.0, new DashStyle([5, 8], 0));

        for (ushort p = PianoRange.FirstPitch; p <= PianoRange.LastPitch; p++)
        {
            NotePitch pitch = new(p);
            double leftX = PianoKeyGeometry.GetColumnLeftX(pitch, panelWidth);
            double width = PianoKeyGeometry.GetColumnWidth(pitch, panelWidth);

            if (pitch.IsAccidental)
            {
                context.DrawLine(dashPen, new Point(leftX, 0), new Point(leftX, panelHeight));
                context.DrawLine(dashPen, new Point(leftX + width, 0), new Point(leftX + width, panelHeight));
            }
            else if (leftX > 0)
            {
                context.DrawLine(solidPen, new Point(leftX, 0), new Point(leftX, panelHeight));
            }
        }
    }
}
