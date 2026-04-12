using System.Collections.Frozen;
using Avalonia.Input;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping;
using Klavier.Config;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Input;

public class KeyboardInputHandler
{
    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private KeyboardMapping _mapping;
    private readonly Dictionary<PhysicalKey, NotePitch> _heldNotes = [];
    private bool _isSustainToggled;

    private UIConfig _lastUiConfig;

    public KeyboardInputHandler(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;
        _mapping = KeyboardMappingProvider.Load(uiConfig.CurrentValue.KeyboardLayout);
        _lastUiConfig = uiConfig.CurrentValue;

        ApplySustainMode(uiConfig.CurrentValue.SustainMode);
        uiConfig.OnChange(OnUIConfigChanged);
    }

    private void OnUIConfigChanged(UIConfig newConfig)
    {
        if (newConfig.SustainMode != _lastUiConfig.SustainMode)
        {
            _isSustainToggled = false;
            ApplySustainMode(newConfig.SustainMode);
        }

        if (newConfig.KeyboardLayout != _lastUiConfig.KeyboardLayout)
        {
            _mapping = KeyboardMappingProvider.Load(newConfig.KeyboardLayout);
            _heldNotes.Clear();
        }
        _lastUiConfig = newConfig;
    }

    private void ApplySustainMode(SustainMode mode)
    {
        _pianoEngine.SustainOff();

        if (mode == SustainMode.InvertedHold)
        {
            _pianoEngine.SustainOn();
        }
    }

    public bool HandleKeyDown(PhysicalKey key, KeyModifiers modifiers)
    {
        return HandleDedicatedKeyDown(key)
            || HandlePianoKeyDown(key, modifiers);
    }

    public bool HandleKeyUp(PhysicalKey key)
    {
        return HandleDedicatedKeyUp(key)
            || HandlePianoKeyUp(key);
    }

    private bool HandleDedicatedKeyDown(PhysicalKey key)
    {
        switch (key)
        {
            case PhysicalKey.Escape:
                _pianoEngine.Panic();
                return true;
            case PhysicalKey.Space:
                HandleSustainKeyDown();
                return true;
            default:
                return false;
        }
    }

    private bool HandleDedicatedKeyUp(PhysicalKey key)
    {
        switch (key)
        {
            case PhysicalKey.Space:
                HandleSustainKeyUp();
                return true;
            default:
                return false;
        }
    }

    private bool HandlePianoKeyDown(PhysicalKey key, KeyModifiers modifiers)
    {
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

    private bool HandlePianoKeyUp(PhysicalKey key)
    {
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
