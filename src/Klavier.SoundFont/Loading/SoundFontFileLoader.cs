using Klavier.Config.Schema;
using Klavier.Config.UserSettings;
using Microsoft.Extensions.Options;

namespace Klavier.SoundFont.Loading;

public class SoundFontFileLoader(
    IUserSettingsService settings,
    IOptionsMonitor<AudioConfig> audioConfig,
    SoundFontInfoCache infoCache) : ISoundFontFileLoader
{
    public Task<bool> TryLoadAsync(string path)
    {
        if (!infoCache.TryReload(path))
        {
            return Task.FromResult(false);
        }

        SoundFontConfig soundFontConfig = audioConfig.CurrentValue.SoundFont;
        (int newBank, int newProgram) = DetermineNewPreset(infoCache.GetSoundFontInfo().Presets, soundFontConfig.Preset);

        settings.UpdateSetting(
            AudioConfig.Keys.SoundFont.Section,
            new { Path = path, Preset = new { Bank = newBank, Program = newProgram } });
        return Task.FromResult(true);
    }

    // Keep the current (Bank, Program) if still present in the new SF; otherwise pick (0, 0)
    // when available, else the lowest available preset key.
    private static (int Bank, int Program) DetermineNewPreset(
        IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> presets,
        SoundFontPresetConfig current)
    {
        (int Bank, int Program) currentKey = (current.Bank, current.Program);
        if (presets.ContainsKey(currentKey))
        {
            return currentKey;
        }
        if (presets.ContainsKey((0, 0)) || presets.Count == 0)
        {
            return (0, 0);
        }
        return presets.Keys.Min();
    }
}
