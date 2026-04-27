using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Core.Engine;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Microsoft.Extensions.Options;
using Klavier.UI.Views.Controls;
using Klavier.Config.Schema;

namespace Klavier.UI.Views.Piano;

public class SustainBarControl : ActivableControl
{
    private const string _SustainKeyLabel = "Space";
    private const string _SustainMusicLabel = "Sustain";

    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly TextBlock _keyLabelText;
    private readonly TextBlock _musicLabelText;

    public SustainBarControl(
        IPianoEngine pianoEngine,
        PianoViewModel pianoViewModel,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;

        _keyLabelText = new TextBlock
        {
            Text = _SustainKeyLabel,
            FontSize = Constants.KeyLabelsFontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _musicLabelText = new TextBlock
        {
            Text = _SustainMusicLabel,
            FontSize = Constants.MusicLabelsFontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Child = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _keyLabelText, _musicLabelText },
        };

        IsActive = pianoViewModel.IsSustainOn;
        pianoViewModel.SustainChanged += isOn => IsActive = isOn;

        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        SolidColorBrush brush = isActive ? ActiveTextBrush : DefaultTextBrush;
        _keyLabelText.Foreground = brush;
        _musicLabelText.Foreground = brush;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
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
                _pianoEngine.ToggleSustain();
                break;
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
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

        e.Handled = true;
    }
}
