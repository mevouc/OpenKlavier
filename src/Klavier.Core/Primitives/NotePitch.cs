namespace Klavier.Core.Primitives;

/// <summary>
/// MIDI note pitch.
/// </summary>
/// <param name="Value">Pitch value (0-127).</param>
public readonly record struct NotePitch(ushort Value)
{
    public const int MinValue = ushort.MinValue; // 0
    public const int MaxValue = 127;
    public const int NotesPerOctave = 12;

    public ushort Value { get; } = Value <= MaxValue
        ? Value
        : throw new ArgumentOutOfRangeException(nameof(Value), Value, "Pitch must be between 0 and 127.");

    public int NoteIndex => Value % NotesPerOctave;
    public int SpnOctave => (Value / NotesPerOctave) - 1;
}
