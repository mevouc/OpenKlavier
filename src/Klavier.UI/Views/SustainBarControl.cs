using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Core.Engine;
using Klavier.UI.Options;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class SustainBarControl : Border
{
    private const string _SustainKeyLabel = "Space";
    private const string _SustainMusicLabel = "Sustain";

    private static readonly SolidColorBrush _UnpressedBrush = new(PianoColors.SustainBar);
    private static readonly SolidColorBrush _PressedBrush = new(PianoColors.SustainBarPressed);
    private static readonly SolidColorBrush _TextBrush = new(Colors.White);

    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private bool _isToggled;

    public SustainBarControl(
        IPianoEngine pianoEngine,
        PianoViewModel pianoViewModel,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;

        Background = pianoViewModel.IsSustainOn ? _PressedBrush : _UnpressedBrush;
        CornerRadius = new CornerRadius(Constants.CornerRadius);

        Child = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = _SustainKeyLabel,
                    Foreground = _TextBrush,
                    FontSize = Constants.KeyLabelsFontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
                new TextBlock
                {
                    Text = _SustainMusicLabel,
                    Foreground = _TextBrush,
                    FontSize = Constants.MusicLabelsFontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            },
        };

        pianoViewModel.SustainChanged += OnSustainChanged;

        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
    }

    private void OnSustainChanged(bool isOn)
    {
        Background = isOn ? _PressedBrush : _UnpressedBrush;
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
                _isToggled = !_isToggled;

                if (_isToggled)
                {
                    _pianoEngine.SustainOn();
                }
                else
                {
                    _pianoEngine.SustainOff();
                }

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
