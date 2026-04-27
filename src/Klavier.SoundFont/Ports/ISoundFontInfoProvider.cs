namespace Klavier.SoundFont.Ports;

public interface ISoundFontInfoProvider
{
    SoundFontInfo GetSoundFontInfo();

    event Action SoundFontInfoChanged;
}
