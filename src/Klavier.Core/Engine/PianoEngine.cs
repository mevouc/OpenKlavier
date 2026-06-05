using Klavier.Core.Events;
using Klavier.Config.Schema;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.Core.Engine;

public class PianoEngine : IPianoEngine
{
    private static readonly InputSource[] _allSources = Enum.GetValues<InputSource>();

    private readonly IOptionsMonitor<PianoConfig> _playbackConfig;
    private readonly ILogger<PianoEngine> _logger;
    private readonly Lock _lock = new();
    private PianoConfig _lastPianoConfig;
    private readonly Dictionary<InputSource, Dictionary<NotePitch, int>> _activeNotesBySource;
    private readonly Dictionary<InputSource, bool> _sustainBySource;
    private readonly HashSet<INoteEventHandler> _noteEventHandlers = [];
    private bool IsSustainOn => _sustainBySource.Values.Any(on => on);

    public event Action? PanicRaised;

    public PianoEngine(
        IOptionsMonitor<PianoConfig> playbackConfig,
        ILogger<PianoEngine> logger)
    {
        _playbackConfig = playbackConfig;
        _logger = logger;

        _activeNotesBySource = _allSources.ToDictionary(source => source, _ => new Dictionary<NotePitch, int>());
        _sustainBySource = _allSources.ToDictionary(source => source, _ => false);

        _lastPianoConfig = _playbackConfig.CurrentValue;
        playbackConfig.OnChange(OnPianoConfigChanged);
    }

    public void RegisterHandler(INoteEventHandler noteEventHandler)
    {
        // Called only at startup before any events flow, so it does not need the lock.
        _noteEventHandlers.Add(noteEventHandler);
    }

    public void NoteOn(
        NotePitch keyPitch,
        NoteVelocity? velocity = null,
        InputSource source = InputSource.User)
    {
        lock (_lock)
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
                NoteOnEvent noteOnEvent = new(keyPitch, soundingPitch, effectiveVelocity, source);
                _logger.LogInformation("Playing note {SoundingPitch} (source {Source})", soundingPitch, source);
                NotifyHandlers(handler => handler.OnNoteOn(noteOnEvent));
            }
        }
    }

    public void NoteOff(NotePitch keyPitch, InputSource source = InputSource.User)
    {
        lock (_lock)
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
    }

    public void AllNotesOff(InputSource source)
    {
        lock (_lock)
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
    }

    public void SustainOn(InputSource source = InputSource.User)
    {
        lock (_lock)
        {
            bool wasOn = IsSustainOn;
            _sustainBySource[source] = true;
            if (!wasOn && IsSustainOn)
            {
                _logger.LogInformation("Sustain on (triggered by {Source})", source);
                NotifyHandlers(handler => handler.OnSustainChanged(true));
            }
        }
    }

    public void SustainOff(InputSource source = InputSource.User)
    {
        lock (_lock)
        {
            bool wasOn = IsSustainOn;
            _sustainBySource[source] = false;
            if (wasOn && !IsSustainOn)
            {
                _logger.LogInformation("Sustain off (triggered by {Source})", source);
                NotifyHandlers(handler => handler.OnSustainChanged(false));
            }
        }
    }

    public void ToggleSustain(InputSource source = InputSource.User)
    {
        lock (_lock)
        {
            if (_sustainBySource[source])
            {
                SustainOff(source);
            }
            else
            {
                SustainOn(source);
            }
        }
    }

    public void Panic()
    {
        lock (_lock)
        {
            foreach (InputSource source in _allSources)
            {
                SustainOff(source);
            }
            PanicAllNotesOff();
        }
        PanicRaised?.Invoke();
    }

    private void PanicAllNotesOff()
    {
        bool anyActive = _allSources.Any(source => _activeNotesBySource[source].Count > 0);
        if (!anyActive)
        {
            return;
        }

        foreach (InputSource source in _allSources)
        {
            _activeNotesBySource[source].Clear();
        }

        _logger.LogInformation("All notes off (panic)");

        for (ushort pitch = NotePitch.MinValue; pitch <= NotePitch.MaxValue; pitch++)
        {
            NotePitch notePitch = new(pitch);
            NotifyHandlers(handler => handler.OnNoteOff(new NoteOffEvent(notePitch, notePitch)));
        }
    }

    private void OnPianoConfigChanged(PianoConfig newConfig)
    {
        lock (_lock)
        {
            if (newConfig.Transpose != _lastPianoConfig.Transpose)
            {
                PanicAllNotesOff();
            }
            _lastPianoConfig = newConfig;
        }
    }

    private void NotifyHandlers(Action<INoteEventHandler> action)
    {
        foreach (INoteEventHandler handler in _noteEventHandlers)
        {
            action(handler);
        }
    }

    private Dictionary<NotePitch, int> GetActiveNotes(InputSource source) => _activeNotesBySource[source];

    private bool IsNoteActive(NotePitch pitch) => _allSources.Any(source => _activeNotesBySource[source].ContainsKey(pitch));
}
