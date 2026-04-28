using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.ViewModels;
using Klavier.UI.Views.Controls;

namespace Klavier.UI.Views.Player;

public class PlayerBarView : DockPanel
{
    private const double _RowHeight = 42;

    // Material Design icon path data (24x24 viewbox)
    private static readonly Geometry _PlayIcon = Geometry.Parse("M8 5v14l11-7z");
    private static readonly Geometry _PauseIcon = Geometry.Parse("M6 19h4V5H6v14zm8-14v14h4V5h-4z");
    private static readonly Geometry _StopIcon = Geometry.Parse("M6 6h12v12H6z");
    private static readonly Geometry _VolumeOnIcon = Geometry.Parse("M3 9v6h4l5 5V4L7 9H3zm13.5 3c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02z");
    private static readonly Geometry _VolumeOffIcon = Geometry.Parse("M3 9v6h4l5 5V4L7 9H3z M15.55 4 L16.5 5.13 L3.95 20 L3 18.87 z");

    private readonly TextBlock _filenameLabel;
    private readonly TextBlock _timeLabel;
    private readonly IconButton _playPauseButton;
    private readonly IconButton _audioToggleButton;

    public PlayerBarView(PlayerViewModel viewModel)
    {
        Height = _RowHeight;

        _filenameLabel = new TextBlock
        {
            Text = viewModel.CurrentScore?.DisplayName ?? "",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0),
        };
        DockPanel.SetDock(_filenameLabel, Dock.Left);
        Children.Add(_filenameLabel);

        StackPanel rightGroup = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 0, 12, 0),
        };

        _playPauseButton = new IconButton(viewModel.IsPlaying ? _PauseIcon : _PlayIcon);
        _playPauseButton.PointerReleased += (_, _) => viewModel.TogglePlayPause();
        rightGroup.Children.Add(_playPauseButton);

        IconButton stopButton = new(_StopIcon);
        stopButton.PointerReleased += (_, _) => viewModel.Stop();
        rightGroup.Children.Add(stopButton);

        _audioToggleButton = new IconButton(viewModel.AudioEnabled ? _VolumeOnIcon : _VolumeOffIcon);
        _audioToggleButton.PointerReleased += (_, _) => viewModel.ToggleAudioEnabled();
        rightGroup.Children.Add(_audioToggleButton);

        DockPanel.SetDock(rightGroup, Dock.Right);
        Children.Add(rightGroup);

        _timeLabel = new TextBlock
        {
            Text = FormatTimeRange(viewModel.Position, viewModel.Duration),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Children.Add(_timeLabel);

        viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(PlayerViewModel.CurrentScore):
                    _filenameLabel.Text = viewModel.CurrentScore?.DisplayName ?? "";
                    break;
                case nameof(PlayerViewModel.Position):
                case nameof(PlayerViewModel.Duration):
                    _timeLabel.Text = FormatTimeRange(viewModel.Position, viewModel.Duration);
                    break;
                case nameof(PlayerViewModel.IsPlaying):
                    _playPauseButton.Glyph = viewModel.IsPlaying ? _PauseIcon : _PlayIcon;
                    break;
                case nameof(PlayerViewModel.AudioEnabled):
                    _audioToggleButton.Glyph = viewModel.AudioEnabled ? _VolumeOnIcon : _VolumeOffIcon;
                    break;
            }
        };
    }

    private static string FormatTimeRange(TimeSpan position, TimeSpan duration)
        => $"{FormatTime(position)} / {FormatTime(duration)}";

    private static string FormatTime(TimeSpan time)
        => $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
}
