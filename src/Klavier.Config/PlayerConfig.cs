namespace Klavier.Config;

public class PlayerConfig
{
    public const string SectionName = "Player";

    public string Path { get; init; } = string.Empty;
    public int LookaheadSeconds { get; init; } = 3;
    public double TempoMultiplier { get; init; } = 1.0;
    public bool AudioEnabled { get; init; } = true;
}
