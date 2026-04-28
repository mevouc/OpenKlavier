namespace Klavier.Config.Schema;

public class PlayerConfig
{
    public const string SectionName = "Player";

    public string Path { get; init; } = string.Empty;
    public int LookaheadSeconds { get; init; } = 3;
    public double TempoMultiplier { get; init; } = 1.0;
    public bool AudioEnabled { get; init; } = true;

    public static class Keys
    {
        public static readonly string Path = ConfigKey.Of(SectionName, nameof(PlayerConfig.Path));
        public static readonly string LookaheadSeconds = ConfigKey.Of(SectionName, nameof(PlayerConfig.LookaheadSeconds));
        public static readonly string TempoMultiplier = ConfigKey.Of(SectionName, nameof(PlayerConfig.TempoMultiplier));
        public static readonly string AudioEnabled = ConfigKey.Of(SectionName, nameof(PlayerConfig.AudioEnabled));
    }
}
