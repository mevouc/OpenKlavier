namespace Klavier.Config;

public class ColorsConfig
{
    public const string SectionName = "Colors";

    public string Accent { get; init; } = "#3A60BF";
    public string WhiteKey { get; init; } = "#FAFAFA";
    public string BlackKey { get; init; } = "#1C1C1C";
    public string KeyBorder { get; init; } = "#333333";
}
