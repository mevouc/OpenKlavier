namespace Klavier.SoundFont.Loading;

public interface ISoundFontFileLoader
{
    Task<bool> TryLoadAsync(string path);
}
