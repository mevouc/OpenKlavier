namespace Klavier.SoundFont;

public interface ISoundFontPresetProvider
{
    IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> GetPresets();

    event Action PresetsChanged;
}
