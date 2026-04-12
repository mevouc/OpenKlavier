using System.Collections.Frozen;
using System.Text.Json;
using Avalonia.Input;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping.Dto;

namespace Klavier.UI.Input.Mapping;

public static class KeyboardMappingProvider
{
    private const string _MappingsFolder = "mappings";

    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string[] GetAvailableLayouts()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, _MappingsFolder);

        return [.. Directory.GetFiles(folder, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Order()!];
    }

    public static KeyboardMapping Load(string layoutName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, _MappingsFolder, $"{layoutName.ToLowerInvariant()}.json");
        string json = File.ReadAllText(path);

        KeyboardMappingDto dto = JsonSerializer.Deserialize<KeyboardMappingDto>(json, _JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize keyboard mapping from '{path}'.");

        return new KeyboardMapping
        {
            WhiteKeys = BuildKeyMap(dto.WhiteKeys),
            BlackKeys = BuildKeyMap(dto.BlackKeys),
            BlackKeyModifier = ParseModifier(dto.BlackKeyModifier),
        };
    }

    private static FrozenDictionary<PhysicalKey, KeyMappingEntry> BuildKeyMap(Dictionary<string, KeyMappingEntryDto> entries)
    {
        Dictionary<PhysicalKey, KeyMappingEntry> map = [];

        foreach ((string keyName, KeyMappingEntryDto entry) in entries)
        {
            if (!Enum.TryParse(keyName, out PhysicalKey physicalKey))
            {
                throw new ArgumentException($"Unknown physical key: '{keyName}'.");
            }

            if (entry.Pitch > NotePitch.MaxValue)
            {
                throw new ArgumentException($"Pitch must be between {NotePitch.MinValue} and {NotePitch.MaxValue}.");
            }

            map[physicalKey] = new KeyMappingEntry(new NotePitch(entry.Pitch), entry.Label);
        }

        return map.ToFrozenDictionary();
    }

    private static KeyModifiers ParseModifier(string modifier)
    {
        return modifier switch
        {
            "Shift" => KeyModifiers.Shift,
            "Ctrl" => KeyModifiers.Control,
            "Alt" => KeyModifiers.Alt,
            _ => throw new ArgumentException($"Unknown black key modifier: '{modifier}'. Expected 'Shift', 'Ctrl', or 'Alt'."),
        };
    }
}
