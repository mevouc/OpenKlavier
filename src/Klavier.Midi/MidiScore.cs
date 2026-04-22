namespace Klavier.Midi;

public record MidiScore(
    string FilePath,
    string? DisplayName,
    TimeSpan TotalDuration,
    IReadOnlyList<MidiNote> Notes,
    IReadOnlyList<MidiSustainEvent> SustainEvents);
