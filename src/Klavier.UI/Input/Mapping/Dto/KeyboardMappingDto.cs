namespace Klavier.UI.Input.Mapping.Dto;

public class KeyboardMappingDto
{
    public string BlackKeyModifier { get; init; } = "Shift";
    public Dictionary<string, KeyMappingEntryDto> WhiteKeys { get; init; } = [];
    public Dictionary<string, KeyMappingEntryDto> BlackKeys { get; init; } = [];
}