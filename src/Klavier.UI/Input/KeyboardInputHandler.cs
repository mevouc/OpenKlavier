using System.Collections.Frozen;
using Avalonia.Input;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;
using Klavier.UI.Options;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Input;

public class KeyboardInputHandler
{
    private static readonly FrozenDictionary<PhysicalKey, NotePitch> _WhiteKeyMap = new Dictionary<PhysicalKey, NotePitch>
    {
        // Digits row: C2 - E3
        [PhysicalKey.Digit1] = new(36),  // C2
        [PhysicalKey.Digit2] = new(38),  // D2
        [PhysicalKey.Digit3] = new(40),  // E2
        [PhysicalKey.Digit4] = new(41),  // F2
        [PhysicalKey.Digit5] = new(43),  // G2
        [PhysicalKey.Digit6] = new(45),  // A2
        [PhysicalKey.Digit7] = new(47),  // B2
        [PhysicalKey.Digit8] = new(48),  // C3
        [PhysicalKey.Digit9] = new(50),  // D3
        [PhysicalKey.Digit0] = new(52),  // E3

        // Top row: F3 - A4
        [PhysicalKey.Q] = new(53),   // F3
        [PhysicalKey.W] = new(55),   // G3
        [PhysicalKey.E] = new(57),   // A3
        [PhysicalKey.R] = new(59),   // B3
        [PhysicalKey.T] = new(60),   // C4
        [PhysicalKey.Y] = new(62),   // D4
        [PhysicalKey.U] = new(64),   // E4
        [PhysicalKey.I] = new(65),   // F4
        [PhysicalKey.O] = new(67),   // G4
        [PhysicalKey.P] = new(69),   // A4

        // Home row: B4 - C6
        [PhysicalKey.A] = new(71),   // B4
        [PhysicalKey.S] = new(72),   // C5
        [PhysicalKey.D] = new(74),   // D5
        [PhysicalKey.F] = new(76),   // E5
        [PhysicalKey.G] = new(77),   // F5
        [PhysicalKey.H] = new(79),   // G5
        [PhysicalKey.J] = new(81),   // A5
        [PhysicalKey.K] = new(83),   // B5
        [PhysicalKey.L] = new(84),   // C6

        // Bottom row: D6 - C7
        [PhysicalKey.Z] = new(86),   // D6
        [PhysicalKey.X] = new(88),   // E6
        [PhysicalKey.C] = new(89),   // F6
        [PhysicalKey.V] = new(91),   // G6
        [PhysicalKey.B] = new(93),   // A6
        [PhysicalKey.N] = new(95),   // B6
        [PhysicalKey.M] = new(96),   // C7
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<PhysicalKey, NotePitch> _BlackKeyMap = new Dictionary<PhysicalKey, NotePitch>
    {
        // Digits row sharps
        [PhysicalKey.Digit1] = new(37),  // C#2
        [PhysicalKey.Digit2] = new(39),  // D#2
        [PhysicalKey.Digit4] = new(42),  // F#2
        [PhysicalKey.Digit5] = new(44),  // G#2
        [PhysicalKey.Digit6] = new(46),  // A#2
        [PhysicalKey.Digit8] = new(49),  // C#3
        [PhysicalKey.Digit9] = new(51),  // D#3

        // Top row sharps
        [PhysicalKey.Q] = new(54),   // F#3
        [PhysicalKey.W] = new(56),   // G#3
        [PhysicalKey.E] = new(58),   // A#3
        [PhysicalKey.T] = new(61),   // C#4
        [PhysicalKey.Y] = new(63),   // D#4
        [PhysicalKey.I] = new(66),   // F#4
        [PhysicalKey.O] = new(68),   // G#4
        [PhysicalKey.P] = new(70),   // A#4

        // Home row sharps
        [PhysicalKey.S] = new(73),   // C#5
        [PhysicalKey.D] = new(75),   // D#5
        [PhysicalKey.G] = new(78),   // F#5
        [PhysicalKey.H] = new(80),   // G#5
        [PhysicalKey.J] = new(82),   // A#5
        [PhysicalKey.L] = new(85),   // C#6

        // Bottom row sharps
        [PhysicalKey.Z] = new(87),   // D#6
        [PhysicalKey.C] = new(90),   // F#6
        [PhysicalKey.V] = new(92),   // G#6
        [PhysicalKey.B] = new(94),   // A#6
    }.ToFrozenDictionary();

    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly Dictionary<PhysicalKey, NotePitch> _heldNotes = [];
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

    public bool HandleKeyDown(PhysicalKey key, KeyModifiers modifiers)
    {
        if (key == PhysicalKey.Space)
        {
            HandleSustainKeyDown();
            return true;
        }

        FrozenDictionary<PhysicalKey, NotePitch> map = modifiers.HasFlag(KeyModifiers.Shift)
            ? _BlackKeyMap
            : _WhiteKeyMap;

        if (map.TryGetValue(key, out NotePitch note) && _heldNotes.TryAdd(key, note))
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
