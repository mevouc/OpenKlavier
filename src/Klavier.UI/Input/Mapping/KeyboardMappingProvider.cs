using System.Collections.Frozen;
using System.Text.Json;
using Avalonia.Input;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping.Dto;

namespace Klavier.UI.Input.Mapping;

public static class KeyboardMappingProvider
{
    private const string _MappingsFolder = "mappings";
    private const string _AppDataFolder = "Klavier";

    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static event Action? LayoutsChanged;

    public static string UserMappingsDirectory
    {
        get
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _AppDataFolder,
                _MappingsFolder);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    private static string AppMappingsDirectory => Path.Combine(AppContext.BaseDirectory, _MappingsFolder);

    public static string[] GetAvailableLayouts()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.GetFiles(AppMappingsDirectory, "*.json"))
        {
            names.Add(Path.GetFileNameWithoutExtension(file));
        }

        foreach (string file in Directory.GetFiles(UserMappingsDirectory, "*.json"))
        {
            names.Add(Path.GetFileNameWithoutExtension(file));
        }

        return [.. names.Order()];
    }

    public static KeyboardMapping Load(string layoutName)
    {
        string fileName = $"{layoutName.ToLowerInvariant()}.json";
        string userPath = Path.Combine(UserMappingsDirectory, fileName);
        string appPath = Path.Combine(AppMappingsDirectory, fileName);
        string path = File.Exists(userPath) ? userPath : appPath;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Keyboard mapping '{layoutName}' not found.", path);
        }

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

    public static void Save(string name, KeyboardMappingDto dto)
    {
        if (!LayoutNameValidator.TryValidate(name, out string? reason))
        {
            throw new ArgumentException(reason, nameof(name));
        }

        string path = Path.Combine(UserMappingsDirectory, $"{name.ToLowerInvariant()}.json");
        string json = JsonSerializer.Serialize(dto, _JsonOptions);
        File.WriteAllText(path, json);

        LayoutsChanged?.Invoke();
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
