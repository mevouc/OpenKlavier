namespace Klavier.SoundFont;

public interface ISoundFontInfoProvider
{
    SoundFontInfo GetSoundFontInfo();

    event Action SoundFontInfoChanged;
}
