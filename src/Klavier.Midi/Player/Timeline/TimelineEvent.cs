namespace Klavier.Midi.Player.Timeline;

public readonly record struct TimelineEvent(
    TimeSpan Time,
    TimelineEventKind Kind,
    MidiNote Note);
