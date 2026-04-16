namespace Klavier.Config;

public class AudioConfig
{
    public const string SectionName = "Audio";

    public SoundFontConfig SoundFont { get; init; } = new();
    public string AudioDriver { get; init; } = "dsound";
    public string MinimumFluidSynthLogLevel { get; init; } = "Error";
    public ushort VolumeInPercent { get; init; } = 60;
    public float GainFactor => VolumeInPercent / 100f;
}
