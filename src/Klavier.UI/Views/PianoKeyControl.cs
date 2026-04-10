using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;

namespace Klavier.UI.Views;

public class PianoKeyControl : Border
{
    private static readonly SolidColorBrush _WhiteKeyBrush = new(PianoColors.WhiteKey);
    private static readonly SolidColorBrush _WhiteKeyPressedBrush = new(PianoColors.WhiteKeyPressed);
    private static readonly SolidColorBrush _BlackKeyBrush = new(PianoColors.BlackKey);
    private static readonly SolidColorBrush _BlackKeyPressedBrush = new(PianoColors.BlackKeyPressed);
    private static readonly SolidColorBrush _BorderBrush = new(PianoColors.KeyBorder);
    private static readonly SolidColorBrush _WhiteKeyTextBrush = new(Colors.Black);
    private static readonly SolidColorBrush _BlackKeyTextBrush = new(Colors.White);

    private readonly PianoKeyViewModel _viewModel;
    private readonly TextBlock _keyLabelText;
    private readonly TextBlock _noteLabelText;

    public PianoKeyControl(PianoKeyViewModel viewModel)
    {
        _viewModel = viewModel;

        BorderBrush = _BorderBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(0, 0, Constants.CornerRadius, Constants.CornerRadius);

        SolidColorBrush textBrush = viewModel.IsBlack ? _BlackKeyTextBrush : _WhiteKeyTextBrush;

        _keyLabelText = new TextBlock
        {
            Text = viewModel.KeyLabel,
            Foreground = textBrush,
            FontSize = Constants.KeyLabelsFontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _noteLabelText = new TextBlock
        {
            Text = viewModel.NoteLabel,
            Foreground = textBrush,
            FontSize = Constants.MusicLabelsFontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Child = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 5),
            Spacing = 2,
            Children = { _keyLabelText, _noteLabelText },
        };

        _keyLabelText.IsVisible = viewModel.ShowKeyLabel;
        _noteLabelText.IsVisible = viewModel.ShowNoteLabel;

        UpdateBackground();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _viewModel.Press();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _viewModel.Release();
        e.Handled = true;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PianoKeyViewModel.IsPressed):
                UpdateBackground();
                break;
            case nameof(PianoKeyViewModel.NoteLabel):
                _noteLabelText.Text = _viewModel.NoteLabel;
                break;
            case nameof(PianoKeyViewModel.ShowKeyLabel):
                _keyLabelText.IsVisible = _viewModel.ShowKeyLabel;
                break;
            case nameof(PianoKeyViewModel.ShowNoteLabel):
                _noteLabelText.IsVisible = _viewModel.ShowNoteLabel;
                break;
        }
    }

    private void UpdateBackground()
    {
        if (_viewModel.IsBlack)
        {
            Background = _viewModel.IsPressed ? _BlackKeyPressedBrush : _BlackKeyBrush;
        }
        else
        {
            Background = _viewModel.IsPressed ? _WhiteKeyPressedBrush : _WhiteKeyBrush;
        }
    }
}
