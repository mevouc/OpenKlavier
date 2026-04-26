using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Klavier.Config;
using Klavier.Midi;
using Klavier.Midi.Player;
using Klavier.UI.Ports;
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

    private readonly IMidiPlayer _player;
    private readonly IUserSettingsService _settings;

    private readonly TextBlock _filenameLabel;
    private readonly TextBlock _timeLabel;
    private readonly IconButton _playPauseButton;
    private readonly IconButton _audioToggleButton;

    public PlayerBarView(IMidiPlayer player, IUserSettingsService settings)
    {
        _player = player;
        _settings = settings;

        Height = _RowHeight;

        _filenameLabel = new TextBlock
        {
            Text = "",
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

        _playPauseButton = new IconButton(_PlayIcon);
        _playPauseButton.PointerReleased += OnPlayPausePressed;
        rightGroup.Children.Add(_playPauseButton);

        IconButton stopButton = new(_StopIcon);
        stopButton.PointerReleased += OnStopPressed;
        rightGroup.Children.Add(stopButton);

        _audioToggleButton = new IconButton(
            _player.AudioEnabled ? _VolumeOnIcon : _VolumeOffIcon);
        _audioToggleButton.PointerReleased += OnAudioTogglePressed;
        rightGroup.Children.Add(_audioToggleButton);

        DockPanel.SetDock(rightGroup, Dock.Right);
        Children.Add(rightGroup);

        _timeLabel = new TextBlock
        {
            Text = "0:00 / 0:00",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Children.Add(_timeLabel);

        if (_player.HasLoadedScore)
        {
            ApplyLoaded(_player.CurrentScore!);
        }
        _player.Loaded += score => Dispatcher.UIThread.Post(() => ApplyLoaded(score));
        _player.Started += () => Dispatcher.UIThread.Post(() => _playPauseButton.Glyph = _PauseIcon);
        _player.Paused += () => Dispatcher.UIThread.Post(() => _playPauseButton.Glyph = _PlayIcon);
        _player.Stopped += () => Dispatcher.UIThread.Post(OnPlayerReset);
        _player.Finished += () => Dispatcher.UIThread.Post(OnPlayerReset);
        _player.Tick += pos => Dispatcher.UIThread.Post(() => UpdateTime(pos));
    }

    private void ApplyLoaded(MidiScore score)
    {
        _filenameLabel.Text = score.DisplayName ?? "";
        _timeLabel.Text = $"{FormatTime(TimeSpan.Zero)} / {FormatTime(score.TotalDuration)}";
    }

    private void OnPlayerReset()
    {
        _playPauseButton.Glyph = _PlayIcon;
        UpdateTime(TimeSpan.Zero);
    }

    private void UpdateTime(TimeSpan position)
    {
        MidiScore? score = _player.CurrentScore;
        if (score is null)
        {
            return;
        }
        _timeLabel.Text = $"{FormatTime(position)} / {FormatTime(score.TotalDuration)}";
    }

    private void OnPlayPausePressed(object? sender, PointerReleasedEventArgs e)
    {
        if (_player.State == MidiPlayerState.Playing)
        {
            _player.Pause();
        }
        else if (_player.HasLoadedScore)
        {
            _player.Play();
        }
    }

    private void OnStopPressed(object? sender, PointerReleasedEventArgs e)
    {
        _player.Stop();
    }

    private void OnAudioTogglePressed(object? sender, PointerReleasedEventArgs e)
    {
        bool newValue = !_player.AudioEnabled;
        _player.AudioEnabled = newValue;
        _audioToggleButton.Glyph = newValue ? _VolumeOnIcon : _VolumeOffIcon;
        _settings.UpdateSetting(
            ConfigKey.Of(PlayerConfig.SectionName, nameof(PlayerConfig.AudioEnabled)),
            newValue);
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
    }
}
