using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
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
        CornerRadius = new CornerRadius(0, 0, 2, 2);

        SolidColorBrush textBrush = viewModel.IsBlack ? _BlackKeyTextBrush : _WhiteKeyTextBrush;

        _keyLabelText = new TextBlock
        {
            Text = viewModel.KeyLabel,
            Foreground = textBrush,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _noteLabelText = new TextBlock
        {
            Text = viewModel.NoteLabel,
            Foreground = textBrush,
            FontSize = 9,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Child = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 5),
            Spacing = 2,
            Children = { _keyLabelText, _noteLabelText },
        };

        UpdateBackground();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PianoKeyViewModel.IsPressed))
        {
            UpdateBackground();
        }
        else if (e.PropertyName == nameof(PianoKeyViewModel.NoteLabel))
        {
            _noteLabelText.Text = _viewModel.NoteLabel;
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
