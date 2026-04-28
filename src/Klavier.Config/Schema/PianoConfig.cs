namespace Klavier.Config.Schema;

public class PianoConfig
{
    public const string SectionName = "Piano";

    public ushort Velocity { get; init; } = 100;
    public short Transpose { get; init; } = 0;

    public static class Keys
    {
        public static readonly string Velocity = ConfigKey.Of(SectionName, nameof(PianoConfig.Velocity));
        public static readonly string Transpose = ConfigKey.Of(SectionName, nameof(PianoConfig.Transpose));
    }
}
