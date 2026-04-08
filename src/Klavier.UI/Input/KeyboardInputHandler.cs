using Avalonia.Input;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;
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

        if (modifiers.HasFlag(_mapping.BlackKeyModifier)
            && _mapping.BlackKeys.TryGetValue(key, out KeyMappingEntry blackKey)
            && _heldNotes.TryAdd(key, blackKey.Pitch))
        {
            _pianoEngine.NoteOn(blackKey.Pitch);
            return true;
        }

        if (_mapping.WhiteKeys.TryGetValue(key, out KeyMappingEntry whiteKey)
            && _heldNotes.TryAdd(key, whiteKey.Pitch))
        {
            _pianoEngine.NoteOn(whiteKey.Pitch);
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
