using System.Collections.Frozen;
using Avalonia.Input;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;
using Klavier.UI.Options;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Input;

public class KeyboardInputHandler
{
    private static readonly FrozenDictionary<PhysicalKey, NotePitch> _KeyToNote = new Dictionary<PhysicalKey, NotePitch>
    {
        [PhysicalKey.T] = new(60),  // C4
        [PhysicalKey.Y] = new(62),  // D4
        [PhysicalKey.U] = new(64),  // E4
        [PhysicalKey.I] = new(65),  // F4
    }.ToFrozenDictionary();

    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly HashSet<PhysicalKey> _heldKeys = [];
    private bool _isSustainToggled;

    public KeyboardInputHandler(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;

        if (uiConfig.CurrentValue.SustainMode == SustainMode.InvertedHold)
        {
            _pianoEngine.SustainOn();
        }
    }

    public bool HandleKeyDown(PhysicalKey key)
    {
        if (key == PhysicalKey.Space)
        {
            HandleSustainKeyDown();
            return true;
        }

        if (_KeyToNote.TryGetValue(key, out NotePitch note) && _heldKeys.Add(key))
        {
            _pianoEngine.NoteOn(note);
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

        if (_KeyToNote.TryGetValue(key, out NotePitch note) && _heldKeys.Remove(key))
        {
            _pianoEngine.NoteOff(note);
            return true;
        }

        return false;
    }

    private void HandleSustainKeyDown()
    {
        SustainMode mode = _uiConfig.CurrentValue.SustainMode;

        switch (mode)
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
        SustainMode mode = _uiConfig.CurrentValue.SustainMode;

        switch (mode)
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
