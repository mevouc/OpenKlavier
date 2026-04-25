using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Klavier.Midi;
using Klavier.Midi.Player;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Player;

public class ProgressBarView : Grid
{
    private const double _Height = 2;

    private readonly IMidiPlayer _player;
    private readonly Rectangle _fill;

    public ProgressBarView(IMidiPlayer player)
    {
        _player = player;

        Height = _Height;
        Background = new SolidColorBrush(ThemePaletteProvider.Divider);

        _fill = new Rectangle
        {
            Fill = new SolidColorBrush(ThemePaletteProvider.Accent),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0,
        };
        Children.Add(_fill);

        player.Loaded += _ => Dispatcher.UIThread.Post(() => _fill.Width = 0);
        player.Tick += OnPlayerTick;
        player.Stopped += () => Dispatcher.UIThread.Post(() => _fill.Width = 0);
        player.Finished += () => Dispatcher.UIThread.Post(() => _fill.Width = 0);
    }

    private void OnPlayerTick(TimeSpan position)
    {
        MidiScore? score = _player.CurrentScore;
        if (score is null || score.TotalDuration <= TimeSpan.Zero)
        {
            return;
        }
        double progress = Math.Clamp(position.TotalSeconds / score.TotalDuration.TotalSeconds, 0, 1);
        Dispatcher.UIThread.Post(() => _fill.Width = Bounds.Width * progress);
    }
}
