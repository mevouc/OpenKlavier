namespace Klavier.Midi.Loading;

public interface IMidiFileLoader
{
    Task<bool> TryLoadAsync(string path);
}
