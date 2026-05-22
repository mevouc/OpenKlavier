using System.Diagnostics;
using Klavier.Config.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.Midi.Playback;

public class MidiPlayer : IMidiPlayer, IDisposable
{
    private const int _TickIntervalMs = 16;
    private const double _MinTempoMultiplier = 0.25;
    private const double _MaxTempoMultiplier = 2.0;
    private readonly Lock _lock = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly HashSet<MidiNote> _activeNotes = [];

    private readonly ILogger<MidiPlayer> _logger;
    private readonly IDisposable? _configSubscription;

    private MidiPlayerState _state = MidiPlayerState.Idle;
    private MidiScore? _currentScore;
    private List<TimelineEvent> _timeline = [];
    private int _timelineIndex;
    private TimeSpan _position;
    private double _tempoMultiplier;
    private bool _audioEnabled;
    private Timer? _timer;

    public MidiPlayer(IOptionsMonitor<PlayerConfig> playerConfig, ILogger<MidiPlayer> logger)
    {
        _logger = logger;

        _tempoMultiplier = Math.Clamp(playerConfig.CurrentValue.TempoMultiplier, _MinTempoMultiplier, _MaxTempoMultiplier);
        _audioEnabled = playerConfig.CurrentValue.AudioEnabled;

        _configSubscription = playerConfig.OnChange(OnPlayerConfigChanged);
    }

    public MidiPlayerState State => _state;
    public MidiScore? CurrentScore => _currentScore;
    public TimeSpan Position => _position;

    public double TempoMultiplier
    {
        get => _tempoMultiplier;
        set => _tempoMultiplier = Math.Clamp(value, _MinTempoMultiplier, _MaxTempoMultiplier);
    }

    public bool AudioEnabled
    {
        get => _audioEnabled;
        set
        {
            if (_audioEnabled == value)
            {
                return;
            }
            _audioEnabled = value;
            AudioEnabledChanged?.Invoke(value);
        }
    }

    public event Action<MidiScore>? Loaded;
    public event Action? Started;
    public event Action? Paused;
    public event Action? Stopped;
    public event Action? Finished;
    public event Action<TimeSpan>? Tick;
    public event Action<PlaybackNoteOn>? NoteOn;
    public event Action<PlaybackNoteOff>? NoteOff;
    public event Action<bool>? SustainChanged;
    public event Action<bool>? AudioEnabledChanged;

    public void Load(MidiScore score)
    {
        lock (_lock)
        {
            if (_state is MidiPlayerState.Playing or MidiPlayerState.Paused)
            {
                DrainAndReset();
            }
            _currentScore = score;
            _timeline = BuildTimeline(score);
            _timelineIndex = 0;
            _position = TimeSpan.Zero;
            _state = MidiPlayerState.Loaded;
            _logger.LogInformation(
                "Loaded score '{DisplayName}' ({NoteCount} notes, {SustainCount} sustain events, duration {Duration})",
                score.DisplayName, score.Notes.Count, score.SustainEvents.Count, score.TotalDuration);
            Loaded?.Invoke(score);
        }
    }

    public void Play()
    {
        lock (_lock)
        {
            if (_currentScore is null)
            {
                throw new InvalidOperationException("Cannot play: no score loaded.");
            }
            if (_state == MidiPlayerState.Playing)
            {
                return;
            }
            _stopwatch.Restart();
            _timer = new Timer(OnTick, null, _TickIntervalMs, _TickIntervalMs);
            _state = MidiPlayerState.Playing;
            _logger.LogInformation("Player started");
            Started?.Invoke();
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_state != MidiPlayerState.Playing)
            {
                return;
            }
            _timer?.Dispose();
            _timer = null;
            _stopwatch.Stop();
            _state = MidiPlayerState.Paused;
            _logger.LogInformation("Player paused at {Position}", _position);
            Paused?.Invoke();
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (_state is not (MidiPlayerState.Playing or MidiPlayerState.Paused))
            {
                return;
            }
            DrainAndReset();
            _state = _currentScore is null ? MidiPlayerState.Idle : MidiPlayerState.Loaded;
            _logger.LogInformation("Player stopped");
            Stopped?.Invoke();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
        _configSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnPlayerConfigChanged(PlayerConfig newConfig)
    {
        lock (_lock)
        {
            _tempoMultiplier = Math.Clamp(newConfig.TempoMultiplier, _MinTempoMultiplier, _MaxTempoMultiplier);
            AudioEnabled = newConfig.AudioEnabled;
        }
    }

    private void OnTick(object? _)
    {
        lock (_lock)
        {
            if (_state != MidiPlayerState.Playing || _currentScore is null)
            {
                return;
            }

            TimeSpan elapsed = _stopwatch.Elapsed;
            _stopwatch.Restart();
            long scaledTicks = (long)(elapsed.Ticks * _tempoMultiplier);
            _position += TimeSpan.FromTicks(scaledTicks);

            while (_timelineIndex < _timeline.Count && _timeline[_timelineIndex].Time <= _position)
            {
                EmitTimelineEvent(_timeline[_timelineIndex]);
                _timelineIndex++;
            }

            Tick?.Invoke(_position);

            if (_position >= _currentScore.TotalDuration)
            {
                FinishInternal();
            }
        }
    }

    private void EmitTimelineEvent(TimelineEvent ev)
    {
        switch (ev.Kind)
        {
            case TimelineEventKind.NoteOff:
                _activeNotes.Remove(ev.Note);
                NoteOff?.Invoke(new PlaybackNoteOff(ev.Note.Pitch));
                break;
            case TimelineEventKind.SustainOff:
                SustainChanged?.Invoke(false);
                break;
            case TimelineEventKind.SustainOn:
                SustainChanged?.Invoke(true);
                break;
            case TimelineEventKind.NoteOn:
                _activeNotes.Add(ev.Note);
                NoteOn?.Invoke(new PlaybackNoteOn(ev.Note.Pitch, ev.Note.Velocity));
                break;
        }
    }

    private void DrainAndReset()
    {
        _timer?.Dispose();
        _timer = null;
        _stopwatch.Stop();

        foreach (MidiNote note in _activeNotes)
        {
            NoteOff?.Invoke(new PlaybackNoteOff(note.Pitch));
        }
        _activeNotes.Clear();
        SustainChanged?.Invoke(false);

        _timelineIndex = 0;
        _position = TimeSpan.Zero;
    }

    private void FinishInternal()
    {
        DrainAndReset();
        _state = MidiPlayerState.Loaded;
        _logger.LogInformation("Player finished");
        Finished?.Invoke();
    }

    private static List<TimelineEvent> BuildTimeline(MidiScore score)
    {
        List<TimelineEvent> timeline = new((score.Notes.Count * 2) + score.SustainEvents.Count);

        foreach (MidiNote note in score.Notes)
        {
            timeline.Add(new TimelineEvent(note.Start, TimelineEventKind.NoteOn, note));
            timeline.Add(new TimelineEvent(note.Start + note.Duration, TimelineEventKind.NoteOff, note));
        }

        foreach (MidiSustainEvent sustainEvent in score.SustainEvents)
        {
            timeline.Add(new TimelineEvent(
                sustainEvent.At,
                sustainEvent.IsOn ? TimelineEventKind.SustainOn : TimelineEventKind.SustainOff,
                default));
        }

        timeline.Sort((a, b) =>
        {
            int timeComparison = a.Time.CompareTo(b.Time);
            return timeComparison != 0
                ? timeComparison
                : ((int)a.Kind).CompareTo((int)b.Kind);
        });

        return timeline;
    }
}
