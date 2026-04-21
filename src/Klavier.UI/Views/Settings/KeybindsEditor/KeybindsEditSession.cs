using Avalonia.Input;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Input.Mapping.Dto;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

/// <summary>
/// Holds the mutable editing state for a keyboard-mapping session and applies edits
/// with all the derived-state rules (black = white + modifier, label normalization, reuse handling).
/// View-agnostic.
/// </summary>
public class KeybindsEditSession(KeyboardMapping source)
{
    private readonly Dictionary<PhysicalKey, KeyMappingEntry> _whiteBindings = new(source.WhiteKeys);
    private readonly Dictionary<PhysicalKey, KeyMappingEntry> _blackBindings = new(source.BlackKeys);

    public IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> WhiteBindings => _whiteBindings;
    public IReadOnlyDictionary<PhysicalKey, KeyMappingEntry> BlackBindings => _blackBindings;
    public KeyModifiers BlackKeyModifier { get; private set; } = source.BlackKeyModifier;
    public bool IsDirty { get; private set; }

    public event Action? BindingsChanged;

    public KeyboardMappingDto ToDto() => new()
    {
        BlackKeyModifier = KeyModifierOptions.LabelOf(BlackKeyModifier),
        WhiteKeys = _whiteBindings.ToDictionary(
            e => e.Key.ToString(),
            e => new KeyMappingEntryDto { Pitch = e.Value.Pitch.Value, Label = e.Value.Label }),
        BlackKeys = _blackBindings.ToDictionary(
            e => e.Key.ToString(),
            e => new KeyMappingEntryDto { Pitch = e.Value.Pitch.Value, Label = e.Value.Label }),
    };

    /// <summary>
    /// Change the layout's black-key modifier. All existing black labels are re-derived with the new
    /// symbol prefix. No-op if the modifier is unchanged.
    /// </summary>
    public void SetModifier(KeyModifiers modifier)
    {
        if (BlackKeyModifier == modifier)
        {
            return;
        }

        BlackKeyModifier = modifier;
        string symbol = KeyModifierOptions.SymbolOf(modifier);

        foreach ((PhysicalKey physicalKey, KeyMappingEntry blackEntry) in _blackBindings.ToList())
        {
            if (_whiteBindings.TryGetValue(physicalKey, out KeyMappingEntry whiteEntry))
            {
                _blackBindings[physicalKey] = new KeyMappingEntry(blackEntry.Pitch, symbol + whiteEntry.Label);
            }
        }

        IsDirty = true;
        BindingsChanged?.Invoke();
    }

    /// <summary>
    /// Apply a binding of <paramref name="physicalKey"/> (with its captured <paramref name="keySymbol"/>)
    /// to <paramref name="whitePitch"/>. Automatically derives the black counterpart when applicable.
    /// Returns a result describing any prior state that was displaced.
    /// </summary>
    public BindingResult Apply(NotePitch whitePitch, PhysicalKey physicalKey, string? keySymbol)
    {
        string label = NormalizeLabel(keySymbol, physicalKey);

        NotePitch? displacedFromPitch = null;
        if (_whiteBindings.TryGetValue(physicalKey, out KeyMappingEntry existing) && existing.Pitch != whitePitch)
        {
            displacedFromPitch = existing.Pitch;
        }

        // Remove any prior PhysicalKey pointing at the new target pitch.
        PhysicalKey? priorKey = FindPhysicalKeyForPitch(whitePitch);
        if (priorKey.HasValue)
        {
            _whiteBindings.Remove(priorKey.Value);
            _blackBindings.Remove(priorKey.Value);
        }

        // The newly chosen PhysicalKey may have been the prior white AND/OR the prior black of another pitch — purge both.
        _whiteBindings.Remove(physicalKey);
        _blackBindings.Remove(physicalKey);

        _whiteBindings[physicalKey] = new KeyMappingEntry(whitePitch, label);

        if (HasBlackCounterpart(whitePitch))
        {
            NotePitch blackPitch = new((ushort)(whitePitch.Value + 1));
            string blackLabel = KeyModifierOptions.SymbolOf(BlackKeyModifier) + label;
            _blackBindings[physicalKey] = new KeyMappingEntry(blackPitch, blackLabel);
        }

        IsDirty = true;
        BindingsChanged?.Invoke();
        return new BindingResult(displacedFromPitch);
    }

    private PhysicalKey? FindPhysicalKeyForPitch(NotePitch pitch)
    {
        foreach ((PhysicalKey key, KeyMappingEntry entry) in _whiteBindings)
        {
            if (entry.Pitch == pitch)
            {
                return key;
            }
        }
        return null;
    }

    private static bool HasBlackCounterpart(NotePitch whitePitch)
    {
        return whitePitch.Value < NotePitch.MaxValue
            && new NotePitch((ushort)(whitePitch.Value + 1)).IsAccidental;
    }

    private static string NormalizeLabel(string? keySymbol, PhysicalKey physicalKey)
    {
        string text = string.IsNullOrEmpty(keySymbol) ? physicalKey.ToString() : keySymbol;
        return text.Length == 1 && text[0] is >= 'a' and <= 'z'
            ? char.ToUpperInvariant(text[0]).ToString()
            : text;
    }

}

public readonly record struct BindingResult(NotePitch? DisplacedFromPitch);
