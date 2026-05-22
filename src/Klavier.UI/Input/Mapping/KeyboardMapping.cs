using System.Collections.Frozen;
using Avalonia.Input;
using Klavier.Core.Primitives;

namespace Klavier.UI.Input.Mapping;

public class KeyboardMapping
{
    public FrozenDictionary<PhysicalKey, KeyMappingEntry> WhiteKeys { get; init; } = FrozenDictionary<PhysicalKey, KeyMappingEntry>.Empty;
    public FrozenDictionary<PhysicalKey, KeyMappingEntry> BlackKeys { get; init; } = FrozenDictionary<PhysicalKey, KeyMappingEntry>.Empty;
    public KeyModifiers BlackKeyModifier { get; init; } = KeyModifiers.Shift;

    public IReadOnlyDictionary<NotePitch, string> ToLabelsByPitch()
    {
        Dictionary<NotePitch, string> labels = [];

        foreach (KeyMappingEntry entry in WhiteKeys.Values)
        {
            labels[entry.Pitch] = entry.Label;
        }

        foreach (KeyMappingEntry entry in BlackKeys.Values)
        {
            labels[entry.Pitch] = entry.Label;
        }

        return labels;
    }
}
