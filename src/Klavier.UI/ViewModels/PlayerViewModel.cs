using CommunityToolkit.Mvvm.ComponentModel;
using Klavier.Config.Schema;
using Klavier.Config.UserSettings;
using Klavier.Midi;
using Klavier.Midi.Playback;
using Klavier.UI.Threading;

namespace Klavier.UI.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IMidiPlayer _player;
    private readonly IUserSettingsService _settings;

    [ObservableProperty]
    public partial TimeSpan Position { get; set; }

    [ObservableProperty]
    public partial TimeSpan Duration { get; set; }

    [ObservableProperty]
    public partial MidiScore? CurrentScore { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool AudioEnabled { get; set; }

    public PlayerViewModel(IMidiPlayer player, IUserSettingsService settings)
    {
        _player = player;
        _settings = settings;

        // Snapshot in case the player was loaded before the VM was constructed - AutoLoadMidi runs at
        // startup before any view (and therefore this VM) is resolved, so the Loaded event fires once
        // with no listener.
        if (player.HasLoadedScore)
        {
            CurrentScore = player.CurrentScore;
            Duration = player.CurrentScore!.TotalDuration;
        }
        AudioEnabled = player.AudioEnabled;
        IsPlaying = player.State == MidiPlayerState.Playing;

        player.Loaded += UIThread.Post<MidiScore>(score =>
        {
            Position = TimeSpan.Zero;
            Duration = score.TotalDuration;
            CurrentScore = score;
        });
        player.Started += UIThread.Post(() => IsPlaying = true);
        player.Paused += UIThread.Post(() => IsPlaying = false);
        player.Stopped += UIThread.Post(OnPlayerReset);
        player.Finished += UIThread.Post(OnPlayerReset);
        player.Tick += UIThread.Post<TimeSpan>(pos => Position = pos);
        player.AudioEnabledChanged += UIThread.Post<bool>(enabled => AudioEnabled = enabled);
    }

    public void TogglePlayPause()
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

    public void Stop() => _player.Stop();

    public void ToggleAudioEnabled()
    {
        bool newValue = !AudioEnabled;
        _player.AudioEnabled = newValue;
        _settings.UpdateSetting(PlayerConfig.Keys.AudioEnabled, newValue);
    }

    private void OnPlayerReset()
    {
        IsPlaying = false;
        Position = TimeSpan.Zero;
    }
}
