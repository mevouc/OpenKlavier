using System.Collections.Frozen;
using Avalonia.Input;

namespace Klavier.UI.Input;

public class KeyboardMapping
{
    public FrozenDictionary<PhysicalKey, KeyMappingEntry> WhiteKeys { get; init; } = FrozenDictionary<PhysicalKey, KeyMappingEntry>.Empty;
    public FrozenDictionary<PhysicalKey, KeyMappingEntry> BlackKeys { get; init; } = FrozenDictionary<PhysicalKey, KeyMappingEntry>.Empty;
    public KeyModifiers BlackKeyModifier { get; init; } = KeyModifiers.Shift;
}
