namespace Klavier.UI.Input;

public class KeyboardMappingDto
{
    public string BlackKeyModifier { get; init; } = "Shift";
    public Dictionary<string, KeyMappingEntryDto> WhiteKeys { get; init; } = [];
    public Dictionary<string, KeyMappingEntryDto> BlackKeys { get; init; } = [];
}

public class KeyMappingEntryDto
{
    public ushort Pitch { get; init; }
    public string Label { get; init; } = "";
}
