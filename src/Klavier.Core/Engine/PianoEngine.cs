using Klavier.Core.Events;
using Klavier.Core.Options;
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
    private bool _isSustainOn;

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

    public void NoteOn(NotePitch keyPitch)
    {
        NoteVelocity velocity = _playbackConfig.CurrentValue.Velocity;

        if (velocity.Value == 0) // MIDI spec: velocity 0 = note-off
        {
            NoteOff(keyPitch);
            return;
        }

        NotePitch soundingPitch = keyPitch.Transpose(_playbackConfig.CurrentValue.Transpose);

        if (_activeNotes.TryAdd(soundingPitch, 1))
        {
            NoteOnEvent noteOnEvent = new(keyPitch, soundingPitch, velocity);

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
        NotePitch soundingPitch = keyPitch.Transpose(_playbackConfig.CurrentValue.Transpose);

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

    public void SustainOn()
    {
        if (_isSustainOn)
        {
            return;
        }

        _isSustainOn = true;
        _logger.LogInformation("Sustain on");
        NotifyHandlers(handler => handler.OnSustainChanged(true));
    }

    public void SustainOff()
    {
        if (!_isSustainOn)
        {
            return;
        }

        _isSustainOn = false;
        _logger.LogInformation("Sustain off");
        NotifyHandlers(handler => handler.OnSustainChanged(false));
    }

    public void AllNotesOff()
    {
        SustainOff();

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
