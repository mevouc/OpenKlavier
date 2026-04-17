namespace Klavier.SoundFont;

public sealed record SoundFontInfo(
    string? Name,
    IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> Presets);
