namespace Klavier.Config.Schema;

public class AudioConfig
{
    public const string SectionName = "Audio";

    public SoundFontConfig SoundFont { get; init; } = new();
    public string AudioDriver { get; init; } = "dsound";
    public string MinimumFluidSynthLogLevel { get; init; } = "Error";
    public ushort VolumeInPercent { get; init; } = 60;
    public float GainFactor => VolumeInPercent / 100f;

    public static class Keys
    {
        public static readonly string VolumeInPercent = ConfigKey.Of(SectionName, nameof(AudioConfig.VolumeInPercent));

        public static class SoundFont
        {
            public static readonly string Section = ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont));
            public static readonly string Path = ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont), nameof(SoundFontConfig.Path));
            public static readonly string Preset = ConfigKey.Of(AudioConfig.SectionName, nameof(AudioConfig.SoundFont), nameof(SoundFontConfig.Preset));
        }
    }
}
