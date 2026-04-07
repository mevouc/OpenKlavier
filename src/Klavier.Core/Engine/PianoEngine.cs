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

    public void NoteOn(NotePitch pitch)
    {
        NoteVelocity velocity = _playbackConfig.CurrentValue.Velocity;

        if (velocity.Value == 0) // MIDI spec: velocity 0 = note-off
        {
            NoteOff(pitch);
            return;
        }

        NotePitch transposedPitch = TransposePitch(pitch);

        if (_activeNotes.TryAdd(transposedPitch, 1))
        {
            NoteOnEvent noteOnEvent = new(transposedPitch, velocity);

            _logger.LogInformation("Playing note {Pitch}", transposedPitch);

            NotifyHandlers(handler => handler.OnNoteOn(noteOnEvent));
        }
        else // note already active
        {
            _activeNotes[transposedPitch]++;
        }
    }

    public void NoteOff(NotePitch pitch)
    {
        NotePitch transposedPitch = TransposePitch(pitch);

        if (_activeNotes.TryGetValue(transposedPitch, out int activeCount))
        {
            if (activeCount == 1)
            {
                _logger.LogInformation("Releasing note {Pitch}", transposedPitch);

                NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(transposedPitch)));

                _activeNotes.Remove(transposedPitch); // notes with 0 active play are removed from dictionary
            }
            else // activeCount > 1
            {
                _activeNotes[transposedPitch] = activeCount - 1;
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

        foreach ((NotePitch transposedPitch, int _) in _activeNotes)
        {
            NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(transposedPitch)));
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

    private NotePitch TransposePitch(NotePitch pitch)
    {
        short transpose = _playbackConfig.CurrentValue.Transpose;

        return new NotePitch((ushort)Math.Clamp(pitch.Value + transpose, NotePitch.MinValue, NotePitch.MaxValue));
    }
}
