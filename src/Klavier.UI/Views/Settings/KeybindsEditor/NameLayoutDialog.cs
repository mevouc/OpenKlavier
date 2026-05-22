using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

public class NameLayoutDialog : Window
{
    private const string _WindowTitle = "Save layout";
    private const int _DialogWidth = 420;
    private const int _DialogHeight = 180;
    private const string _PromptLabel = "Layout name:";
    private const string _SaveButtonLabel = "Save";
    private const string _CancelButtonLabel = "Cancel";
    private const string _OverwriteWarning = "A custom layout with this name already exists. Saving will overwrite it.";
    private const double _RootMargin = 16;
    private const double _ControlSpacing = 8;
    private const double _ButtonsRowSpacing = 8;

    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.Inverse);
    private static readonly SolidColorBrush _WarningBrush = new(Colors.Orange);
    private static readonly SolidColorBrush _ErrorBrush = new(Colors.IndianRed);

    private readonly IKeyboardMappingService _keyboardMappingService;
    private readonly TextBox _nameBox;
    private readonly TextBlock _feedbackText;
    private readonly TextButton _saveButton;
    private readonly TextButton _cancelButton;

    /// <summary>
    /// The confirmed layout name after the dialog closes, or null if the user cancelled.
    /// </summary>
    public string? ConfirmedName { get; private set; }

    public NameLayoutDialog(string? prefilledName, IKeyboardMappingService keyboardMappingService)
    {
        _keyboardMappingService = keyboardMappingService;

        Title = _WindowTitle;
        Width = _DialogWidth;
        Height = _DialogHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.MediumContrasted);
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        _nameBox = new TextBox
        {
            Text = prefilledName ?? string.Empty,
            Foreground = _TextBrush,
            FontSize = Constants.PrimaryFontSize,
        };
        _nameBox.TextChanged += (_, _) => UpdateFeedback();

        _feedbackText = new TextBlock
        {
            FontSize = Constants.PrimaryFontSize,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false,
            Margin = new Thickness(0, _ControlSpacing, 0, _ControlSpacing),
        };

        _saveButton = new TextButton(_SaveButtonLabel);
        _saveButton.PointerPressed += (_, e) =>
        {
            Confirm();
            e.Handled = true;
        };

        _cancelButton = new TextButton(_CancelButtonLabel);
        _cancelButton.PointerPressed += (_, e) =>
        {
            Close();
            e.Handled = true;
        };

        Content = BuildLayout();
        KeyDown += OnDialogKeyDown;
        UpdateFeedback();
    }

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Confirm();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void Confirm()
    {
        if (!_saveButton.IsEnabled)
        {
            return;
        }
        ConfirmedName = _nameBox.Text;
        Close();
    }

    private void UpdateFeedback()
    {
        string? name = _nameBox.Text;
        if (!LayoutNameValidator.TryValidate(name, out string? reason))
        {
            _feedbackText.Text = reason ?? "Invalid name.";
            _feedbackText.Foreground = _ErrorBrush;
            _feedbackText.IsVisible = true;
            _saveButton.IsEnabled = false;
            return;
        }

        if (UserLayoutExists(name!))
        {
            _feedbackText.Text = _OverwriteWarning;
            _feedbackText.Foreground = _WarningBrush;
            _feedbackText.IsVisible = true;
        }
        else
        {
            _feedbackText.IsVisible = false;
        }
        _saveButton.IsEnabled = true;
    }

    private bool UserLayoutExists(string name) => _keyboardMappingService.UserLayoutExists(name);

    private Grid BuildLayout()
    {
        TextBlock prompt = new()
        {
            Text = _PromptLabel,
            Foreground = _TextBrush,
            FontSize = Constants.PrimaryFontSize,
            Margin = new Thickness(0, 0, 0, _ControlSpacing),
        };

        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = _ButtonsRowSpacing,
            Children = { _cancelButton, _saveButton },
        };

        Grid root = new()
        {
            Margin = new Thickness(_RootMargin),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
            },
        };

        Grid.SetRow(prompt, 0);
        Grid.SetRow(_nameBox, 1);
        Grid.SetRow(_feedbackText, 2);
        Grid.SetRow(buttons, 3);

        root.Children.Add(prompt);
        root.Children.Add(_nameBox);
        root.Children.Add(_feedbackText);
        root.Children.Add(buttons);

        return root;
    }
}
