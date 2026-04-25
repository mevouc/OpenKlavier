using Klavier.Core.Events;
using Klavier.Config;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Klavier.Core.Engine;

public class PianoEngine : IPianoEngine
{
    private readonly IOptionsMonitor<PianoConfig> _playbackConfig;
    private readonly ILogger<PianoEngine> _logger;
    private PianoConfig _lastPianoConfig;
    private readonly Dictionary<NotePitch, int> _userActiveNotes = [];
    private readonly Dictionary<NotePitch, int> _playerActiveNotes = [];
    private readonly HashSet<INoteEventHandler> _noteEventHandlers = [];
    private bool _userSustainOn;
    private bool _playerSustainOn;
    private bool IsSustainOn => _userSustainOn || _playerSustainOn;

    public event Action? PanicRaised;

    public PianoEngine(
        IOptionsMonitor<PianoConfig> playbackConfig,
        ILogger<PianoEngine> logger)
    {
        _playbackConfig = playbackConfig;
        _logger = logger;

        _lastPianoConfig = _playbackConfig.CurrentValue;
        playbackConfig.OnChange(OnPianoConfigChanged);
    }

    public void RegisterHandler(INoteEventHandler noteEventHandler)
    {
        _noteEventHandlers.Add(noteEventHandler);
    }

    public void NoteOn(
        NotePitch keyPitch,
        NoteVelocity? velocity = null,
        InputSource source = InputSource.User)
    {
        NoteVelocity effectiveVelocity = velocity ?? new NoteVelocity(_playbackConfig.CurrentValue.Velocity);

        if (effectiveVelocity.Value == 0) // MIDI spec: velocity 0 = note-off
        {
            NoteOff(keyPitch, source);
            return;
        }

        NotePitch soundingPitch = keyPitch.Transpose(new Transpose(_playbackConfig.CurrentValue.Transpose));
        bool wasActive = IsNoteActive(soundingPitch);
        Dictionary<NotePitch, int> sourceActiveNotes = GetActiveNotes(source);

        if (!sourceActiveNotes.TryAdd(soundingPitch, 1))
        {
            sourceActiveNotes[soundingPitch]++;
        }

        if (!wasActive)
        {
            NoteOnEvent noteOnEvent = new(keyPitch, soundingPitch, effectiveVelocity);
            _logger.LogInformation("Playing note {SoundingPitch} (source {Source})", soundingPitch, source);
            NotifyHandlers(handler => handler.OnNoteOn(noteOnEvent));
        }
    }

    public void NoteOff(NotePitch keyPitch, InputSource source = InputSource.User)
    {
        NotePitch soundingPitch = keyPitch.Transpose(new Transpose(_playbackConfig.CurrentValue.Transpose));
        Dictionary<NotePitch, int> sourceActiveNotes = GetActiveNotes(source);

        if (sourceActiveNotes.TryGetValue(soundingPitch, out int activeCount))
        {
            if (activeCount == 1)
            {
                sourceActiveNotes.Remove(soundingPitch);

                if (!IsNoteActive(soundingPitch))
                {
                    _logger.LogInformation("Releasing note {SoundingPitch}", soundingPitch);
                    NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(keyPitch, soundingPitch)));
                }
            }
            else
            {
                sourceActiveNotes[soundingPitch] = activeCount - 1;
            }
        }
    }

    public void AllNotesOff(InputSource source)
    {
        Dictionary<NotePitch, int> sourceDict = GetActiveNotes(source);
        if (sourceDict.Count == 0)
        {
            return;
        }

        List<NotePitch> affectedPitches = [.. sourceDict.Keys];
        sourceDict.Clear();

        _logger.LogInformation("All notes off (source {Source})", source);

        foreach (NotePitch pitch in affectedPitches.Where(pitch => !IsNoteActive(pitch)))
        {
            NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(pitch, pitch)));
        }
    }

    public void SustainOn(InputSource source = InputSource.User)
    {
        bool wasOn = IsSustainOn;
        switch (source)
        {
            case InputSource.User:
                _userSustainOn = true;
                break;
            case InputSource.Playback:
                _playerSustainOn = true;
                break;
        }
        if (!wasOn && IsSustainOn)
        {
            _logger.LogInformation("Sustain on (triggered by {Source})", source);
            NotifyHandlers(handler => handler.OnSustainChanged(true));
        }
    }

    public void SustainOff(InputSource source = InputSource.User)
    {
        bool wasOn = IsSustainOn;
        switch (source)
        {
            case InputSource.User:
                _userSustainOn = false;
                break;
            case InputSource.Playback:
                _playerSustainOn = false;
                break;
        }
        if (wasOn && !IsSustainOn)
        {
            _logger.LogInformation("Sustain off (triggered by {Source})", source);
            NotifyHandlers(handler => handler.OnSustainChanged(false));
        }
    }

    public void ToggleSustain(InputSource source = InputSource.User)
    {
        bool isSourceOn = source switch
        {
            InputSource.User => _userSustainOn,
            InputSource.Playback => _playerSustainOn,
            _ => false,
        };
        if (isSourceOn)
        {
            SustainOff(source);
        }
        else
        {
            SustainOn(source);
        }
    }

    public void Panic()
    {
        SustainOff(InputSource.User);
        SustainOff(InputSource.Playback);
        PanicAllNotesOff();
        PanicRaised?.Invoke();
    }

    private void PanicAllNotesOff()
    {
        if (_userActiveNotes.Count == 0 && _playerActiveNotes.Count == 0)
        {
            return;
        }

        _logger.LogInformation("All notes off (panic)");
        _userActiveNotes.Clear();
        _playerActiveNotes.Clear();

        for (ushort pitch = NotePitch.MinValue; pitch <= NotePitch.MaxValue; pitch++)
        {
            NotePitch notePitch = new(pitch);
            NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(notePitch, notePitch)));
        }
    }

    private void OnPianoConfigChanged(PianoConfig newConfig)
    {
        if (newConfig.Transpose != _lastPianoConfig.Transpose)
        {
            PanicAllNotesOff();
        }
        _lastPianoConfig = newConfig;
    }

    private void NotifyHandlers(Action<INoteEventHandler> action)
    {
        foreach (INoteEventHandler handler in _noteEventHandlers)
        {
            action(handler);
        }
    }

    private Dictionary<NotePitch, int> GetActiveNotes(InputSource source) => source switch
    {
        InputSource.User => _userActiveNotes,
        InputSource.Playback => _playerActiveNotes,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown input source"),
    };

    private bool IsNoteActive(NotePitch pitch) =>
        _userActiveNotes.ContainsKey(pitch) || _playerActiveNotes.ContainsKey(pitch);
}
