using Klavier.Core.Events;

namespace Klavier.Midi.Player;

public interface IMidiPlayer
{
    MidiPlayerState State { get; }
    MidiScore? CurrentScore { get; }
    TimeSpan Position { get; }
    double TempoMultiplier { get; set; }
    bool AudioEnabled { get; set; }

    void Load(MidiScore score);
    void Play();
    void Pause();
    void Stop();

    event Action<MidiScore>? Loaded;
    event Action? Started;
    event Action? Paused;
    event Action? Stopped;
    event Action? Finished;
    event Action<TimeSpan>? Tick;
    event Action<NoteOnEvent>? NoteOn;
    event Action<NoteOffEvent>? NoteOff;
    event Action<bool>? SustainChanged;
}
