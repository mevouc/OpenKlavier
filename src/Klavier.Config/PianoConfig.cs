namespace Klavier.Config;

public class PianoConfig
{
    public const string SectionName = "Piano";

    public ushort Velocity { get; init; } = 100;
    public short Transpose { get; init; } = 0;
}
