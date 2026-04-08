using System.Collections.Frozen;
using Avalonia.Input;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Options;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Input;

public class KeyboardInputHandler
{
    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly KeyboardMapping _mapping;
    private readonly Dictionary<PhysicalKey, NotePitch> _heldNotes = [];
    private bool _isSustainToggled;

    public KeyboardInputHandler(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;
        _mapping = KeyboardMappingProvider.Load(uiConfig.CurrentValue.KeyboardLayout);

        if (uiConfig.CurrentValue.SustainMode == SustainMode.InvertedHold)
        {
            _pianoEngine.SustainOn();
        }
    }

    public bool HandleKeyDown(PhysicalKey key, KeyModifiers modifiers)
    {
        if (key == PhysicalKey.Space)
        {
            HandleSustainKeyDown();
            return true;
        }

        FrozenDictionary<PhysicalKey, KeyMappingEntry> map = modifiers.HasFlag(_mapping.BlackKeyModifier)
            ? _mapping.BlackKeys
            : _mapping.WhiteKeys;

        if (map.TryGetValue(key, out KeyMappingEntry keyEntry) && _heldNotes.TryAdd(key, keyEntry.Pitch))
        {
            _pianoEngine.NoteOn(keyEntry.Pitch);
            return true;
        }

        return false;
    }

    public bool HandleKeyUp(PhysicalKey key)
    {
        if (key == PhysicalKey.Space)
        {
            HandleSustainKeyUp();
            return true;
        }

        if (_heldNotes.Remove(key, out NotePitch note))
        {
            _pianoEngine.NoteOff(note);
            return true;
        }

        return false;
    }

    private void HandleSustainKeyDown()
    {
        switch (_uiConfig.CurrentValue.SustainMode)
        {
            case SustainMode.Hold:
                _pianoEngine.SustainOn();
                break;
            case SustainMode.InvertedHold:
                _pianoEngine.SustainOff();
                break;
            case SustainMode.Toggle:
                _isSustainToggled = !_isSustainToggled;

                if (_isSustainToggled)
                {
                    _pianoEngine.SustainOn();
                }
                else
                {
                    _pianoEngine.SustainOff();
                }

                break;
        }
    }

    private void HandleSustainKeyUp()
    {
        switch (_uiConfig.CurrentValue.SustainMode)
        {
            case SustainMode.Hold:
                _pianoEngine.SustainOff();
                break;
            case SustainMode.InvertedHold:
                _pianoEngine.SustainOn();
                break;
        }
    }
}
