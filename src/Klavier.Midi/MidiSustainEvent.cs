namespace Klavier.Midi;

public readonly record struct MidiSustainEvent(TimeSpan At, bool IsOn);
