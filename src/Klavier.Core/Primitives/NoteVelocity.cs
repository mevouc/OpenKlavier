namespace Klavier.Core.Primitives;

/// <summary>
/// MIDI note velocity (how hard a key is struck).
/// </summary>
/// <param name="Value">Velocity value (0-127). 0 is equivalent to note-off per MIDI spec.</param>
public readonly record struct NoteVelocity(ushort Value)
{
    public const ushort MinValue = ushort.MinValue; // 0
    public const ushort MaxValue = 127;

    public ushort Value { get; } = Value <= MaxValue
        ? Value
        : throw new ArgumentOutOfRangeException(nameof(Value), Value, "Velocity must be between 0 and 127.");
}
