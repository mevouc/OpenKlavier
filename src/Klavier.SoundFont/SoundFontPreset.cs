namespace Klavier.SoundFont;

public readonly record struct SoundFontPreset(
    int Bank,
    int Program,
    string Name)
{
    public override string ToString() => $"{Bank}:{Program} - {Name}";
}
