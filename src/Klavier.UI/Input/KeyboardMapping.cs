namespace Klavier.UI.Input;

public class KeyboardMapping
{
    public string BlackKeyModifier { get; init; } = "Shift";
    public Dictionary<string, KeyMappingEntry> WhiteKeys { get; init; } = [];
    public Dictionary<string, KeyMappingEntry> BlackKeys { get; init; } = [];
}

public class KeyMappingEntry
{
    public ushort Pitch { get; init; }
    public string Label { get; init; } = "";
}
