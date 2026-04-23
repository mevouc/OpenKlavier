using Klavier.Core.Events;
using Klavier.Config;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.Core.Engine;

public class PianoEngine : IPianoEngine
{
    private readonly IOptionsMonitor<PianoConfig> _playbackConfig;
    private readonly ILogger<PianoEngine> _logger;
    private PianoConfig _lastPianoConfig;
    private readonly Dictionary<NotePitch, int> _activeNotes = []; // value is active inputs count (note plays when there's at least one)
    private readonly HashSet<INoteEventHandler> _noteEventHandlers = [];
    private bool _userSustainOn;
    private bool _playerSustainOn;
    private bool _isSustainOn => _userSustainOn || _playerSustainOn;

    public event Action? PanicRaised;

    public PianoEngine(
        IOptionsMonitor<PianoConfig> playbackConfig,
        ILogger<PianoEngine> logger)
    {
        _playbackConfig = playbackConfig;
        _logger = logger;

        _lastPianoConfig = _playbackConfig.CurrentValue;
        playbackConfig.OnChange(OnPianoConfigChanged); // triggers AllNotesOff if transpose changes
    }

    public void RegisterHandler(INoteEventHandler noteEventHandler)
    {
        _noteEventHandlers.Add(noteEventHandler);
    }

    public void NoteOn(NotePitch keyPitch, NoteVelocity? velocity = null)
    {
        NoteVelocity effectiveVelocity = velocity ?? new NoteVelocity(_playbackConfig.CurrentValue.Velocity);

        if (effectiveVelocity.Value == 0) // MIDI spec: velocity 0 = note-off
        {
            NoteOff(keyPitch);
            return;
        }

        NotePitch soundingPitch = keyPitch.Transpose(new Transpose(_playbackConfig.CurrentValue.Transpose));

        if (_activeNotes.TryAdd(soundingPitch, 1))
        {
            NoteOnEvent noteOnEvent = new(keyPitch, soundingPitch, effectiveVelocity);

            _logger.LogInformation("Playing note {SoundingPitch}", soundingPitch);

            NotifyHandlers(handler => handler.OnNoteOn(noteOnEvent));
        }
        else // note already active
        {
            _activeNotes[soundingPitch]++;
        }
    }

    public void NoteOff(NotePitch keyPitch)
    {
        NotePitch soundingPitch = keyPitch.Transpose(new Transpose(_playbackConfig.CurrentValue.Transpose));

        if (_activeNotes.TryGetValue(soundingPitch, out int activeCount))
        {
            if (activeCount == 1)
            {
                _logger.LogInformation("Releasing note {SoundingPitch}", soundingPitch);

                NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(keyPitch, soundingPitch)));

                _activeNotes.Remove(soundingPitch); // notes with 0 active play are removed from dictionary
            }
            else // activeCount > 1
            {
                _activeNotes[soundingPitch] = activeCount - 1;
            }
        }
    }

    public void SustainOn(InputSource source = InputSource.User)
    {
        bool wasOn = _isSustainOn;
        switch (source)
        {
            case InputSource.User:
                _userSustainOn = true;
                break;
            case InputSource.Playback:
                _playerSustainOn = true;
                break;
        }
        if (!wasOn && _isSustainOn)
        {
            _logger.LogInformation("Sustain on (triggered by {Source})", source);
            NotifyHandlers(handler => handler.OnSustainChanged(true));
        }
    }

    public void SustainOff(InputSource source = InputSource.User)
    {
        bool wasOn = _isSustainOn;
        switch (source)
        {
            case InputSource.User:
                _userSustainOn = false;
                break;
            case InputSource.Playback:
                _playerSustainOn = false;
                break;
        }
        if (wasOn && !_isSustainOn)
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
        AllNotesOff();
        PanicRaised?.Invoke();
    }

    private void AllNotesOff()
    {
        if (_activeNotes.Count == 0)
        {
            return;
        }
        _logger.LogInformation("All notes off");

        for (ushort pitch = NotePitch.MinValue; pitch <= NotePitch.MaxValue; pitch++)
        {
            NotePitch notePitch = new(pitch);
            NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(notePitch, notePitch)));
        }

        _activeNotes.Clear();
    }

    private void OnPianoConfigChanged(PianoConfig newConfig)
    {
        if (newConfig.Transpose != _lastPianoConfig.Transpose)
        {
            AllNotesOff();
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
}
