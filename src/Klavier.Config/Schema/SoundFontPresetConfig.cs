namespace Klavier.Config.Schema;

public class SoundFontPresetConfig
{
    public const string SectionName = "Preset";

    public int Bank { get; init; } = 0;
    public int Program { get; init; } = 0;
}
