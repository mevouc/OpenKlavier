using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Core.Engine;
using Klavier.Config;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Piano;

public class SustainBarControl : Border
{
    private const string _SustainKeyLabel = "Space";
    private const string _SustainMusicLabel = "Sustain";

    private static readonly SolidColorBrush _BackgroundBrush = new(KlavierTheme.PanelBackground);
    private static readonly SolidColorBrush _DefaultBorderBrush = new(KlavierTheme.PanelBackground);
    private static readonly SolidColorBrush _ActiveBorderBrush = new(KlavierTheme.Accent);
    private static readonly SolidColorBrush _DefaultTextBrush = new(Colors.White);
    private static readonly SolidColorBrush _ActiveTextBrush = new(KlavierTheme.Accent);

    private readonly IPianoEngine _pianoEngine;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly TextBlock _keyLabelText;
    private readonly TextBlock _musicLabelText;
    private bool _isToggled;

    public SustainBarControl(
        IPianoEngine pianoEngine,
        PianoViewModel pianoViewModel,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _pianoEngine = pianoEngine;
        _uiConfig = uiConfig;

        Background = _BackgroundBrush;
        BorderThickness = new Thickness(2);
        CornerRadius = new CornerRadius(Constants.CornerRadius);

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

        UpdateVisualState(pianoViewModel.IsSustainOn);
        pianoViewModel.SustainChanged += OnSustainChanged;

        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
    }

    private void OnSustainChanged(bool isOn)
    {
        UpdateVisualState(isOn);
    }

    private void UpdateVisualState(bool isActive)
    {
        BorderBrush = isActive ? _ActiveBorderBrush : _DefaultBorderBrush;
        _keyLabelText.Foreground = isActive ? _ActiveTextBrush : _DefaultTextBrush;
        _musicLabelText.Foreground = isActive ? _ActiveTextBrush : _DefaultTextBrush;
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
