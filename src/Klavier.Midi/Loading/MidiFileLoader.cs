using Klavier.Config;
using Klavier.Config.Schema;
using Klavier.Config.UserSettings;
using Klavier.Midi.Parsing;
using Klavier.Midi.Playback;
using Microsoft.Extensions.Logging;

namespace Klavier.Midi.Loading;

public class MidiFileLoader(
    IMidiScoreLoader loader,
    IMidiPlayer player,
    IUserSettingsService settings,
    ILogger<MidiFileLoader> logger) : IMidiFileLoader
{
    public async Task<bool> TryLoadAsync(string path)
    {
        MidiScore score;
        try
        {
            score = await loader.LoadAsync(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load MIDI file {Path}", path);
            return false;
        }

        player.Load(score);
        settings.UpdateSetting(
            ConfigKey.Of(PlayerConfig.SectionName, nameof(PlayerConfig.Path)),
            path);
        return true;
    }
}
