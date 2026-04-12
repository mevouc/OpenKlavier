namespace Klavier.Core.Primitives;

/// <summary>
/// Semitone transposition offset applied to piano keys.
/// </summary>
/// <param name="Value">Transpose value (-24 to +24 semitones).</param>
public readonly record struct Transpose(short Value)
{
    public const short MinValue = -24;
    public const short MaxValue = 24;

    public short Value { get; } = Value >= MinValue && Value <= MaxValue
        ? Value
        : throw new ArgumentOutOfRangeException(nameof(Value), Value, $"Transpose must be between {MinValue} and {MaxValue}.");
}
