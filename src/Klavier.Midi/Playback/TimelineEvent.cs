namespace Klavier.Midi.Playback;

public readonly record struct TimelineEvent(
    TimeSpan Time,
    TimelineEventKind Kind,
    MidiNote Note);
