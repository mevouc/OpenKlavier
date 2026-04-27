using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Klavier.Config.Schema;
using Klavier.Core.Primitives;
using Klavier.Midi;
using Klavier.Midi.Playback;
using Klavier.UI.Theme;
using Klavier.UI.Views.Piano;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Player;

public class FallingNotesView : Control
{
    private static readonly SolidColorBrush _NotesColorBrush = new(ThemePaletteProvider.Accent);

    private readonly IMidiPlayer _player;
    private readonly IOptionsMonitor<PlayerConfig> _playerConfig;

    public FallingNotesView(IMidiPlayer player, IOptionsMonitor<PlayerConfig> playerConfig)
    {
        _player = player;
        _playerConfig = playerConfig;

        ClipToBounds = true;

        _player.Tick += _ => Dispatcher.UIThread.Post(InvalidateVisual);
        _player.Loaded += _ => Dispatcher.UIThread.Post(InvalidateVisual);
        _player.Stopped += () => Dispatcher.UIThread.Post(InvalidateVisual);
        _player.Finished += () => Dispatcher.UIThread.Post(InvalidateVisual);
        _playerConfig.OnChange(_ => Dispatcher.UIThread.Post(InvalidateVisual));
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

        MidiScore? score = _player.CurrentScore;
        if (score is null)
        {
            return;
        }

        double lookaheadSeconds = _playerConfig.CurrentValue.LookaheadSeconds;
        if (lookaheadSeconds <= 0)
        {
            return;
        }

        TimeSpan position = _player.Position;

        foreach (MidiNote note in score.Notes)
        {
            double secondsUntilStart = (note.Start - position).TotalSeconds;
            double secondsUntilEnd = (note.Start + note.Duration - position).TotalSeconds;

            if (secondsUntilEnd <= 0 // fully past the piano
                || secondsUntilStart >= lookaheadSeconds // too far in the future
                || !PianoKeyGeometry.IsInRange(note.Pitch))
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
