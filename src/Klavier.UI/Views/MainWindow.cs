using System.Collections.Frozen;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;
using Klavier.UI.Options;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class MainWindow : Window
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
    private readonly HashSet<PhysicalKey> _heldKeys = []; // physical keyboard scan codes, based on QWERTY mapping
    private bool _isSustainToggled;

    public MainWindow(IPianoEngine pianoEngine, PianoView pianoView, IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;

        Title = "Klavier";
        Width = 1000;
        Height = 300;
        Background = new SolidColorBrush(KlavierTheme.AppBackground);
        Topmost = uiConfig.CurrentValue.Topmost;

        uiConfig.OnChange(config => Topmost = config.Topmost);

        Content = pianoView;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Space)
        {
            HandleSustainKeyDown();
            e.Handled = true;
        }
        else if (_KeyToNote.TryGetValue(e.PhysicalKey, out NotePitch note) && _heldKeys.Add(e.PhysicalKey))
        {
            _pianoEngine.NoteOn(note);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Space)
        {
            HandleSustainKeyUp();
            e.Handled = true;
        }
        else if (_KeyToNote.TryGetValue(e.PhysicalKey, out NotePitch note) && _heldKeys.Remove(e.PhysicalKey))
        {
            _pianoEngine.NoteOff(note);
            e.Handled = true;
        }

        base.OnKeyUp(e);
    }

    private void HandleSustainKeyDown()
    {
        SustainMode mode = _uiConfig.CurrentValue.SustainMode;

        if (mode == SustainMode.Hold)
        {
            _pianoEngine.SustainOn();
        }
        else // Toggle
        {
            _isSustainToggled = !_isSustainToggled;

            if (_isSustainToggled)
            {
                _pianoEngine.SustainOn();
            }
            else
            {
                _pianoEngine.SustainOff();
            }
        }
    }

    private void HandleSustainKeyUp()
    {
        SustainMode mode = _uiConfig.CurrentValue.SustainMode;

        if (mode == SustainMode.Hold)
        {
            _pianoEngine.SustainOff();
        }
    }
}
