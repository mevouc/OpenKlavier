namespace Klavier.Config;

public class SoundFontConfig
{
    public const string SectionName = "SoundFont";

    public string Path { get; init; } = "C:\\Users\\mevouc\\Desktop\\GRAND PIANO.sf2";
    public SoundFontPresetConfig Preset { get; init; } = new();
}
