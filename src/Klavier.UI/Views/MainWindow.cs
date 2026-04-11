using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Klavier.UI.Input;
using Klavier.Config;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private const string _WindowTitle = "Klavier";
    private const int _DefaultWidth = 1000;
    private const int _DefaultHeight = 300;
    private const int _MinWidth = 700;
    private const int _MinHeight = 150;
    private const int _SettingsMinHeight = 150;
    private const int _SplitterHeight = 1;
    private const int _DefaultSettingsHeight = 200;

    private readonly KeyboardInputHandler _keyboardInput;
    private readonly RowDefinition _splitterRow;
    private readonly RowDefinition _settingsRow;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        ToolbarView toolbarView,
        SettingsPanel settingsPanel,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _keyboardInput = keyboardInput;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(KlavierTheme.AppBackground);
        Topmost = uiConfig.CurrentValue.Topmost;

        uiConfig.OnChange(config => Topmost = config.Topmost);

        // Top section: piano + toolbar
        Border separatorLine = new()
        {
            Height = 1,
            Background = new SolidColorBrush(KlavierTheme.Divider),
        };

        Grid separator = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
        };
        Grid.SetColumn(separatorLine, 1);
        separator.Children.Add(separatorLine);

        DockPanel.SetDock(toolbarView, Dock.Bottom);
        DockPanel.SetDock(separator, Dock.Bottom);

        DockPanel topSection = new()
        {
            Children = { toolbarView, separator, pianoView },
        };

        // Grid splitter
        GridSplitter splitter = new()
        {
            Height = _SplitterHeight,
            MinHeight = _SplitterHeight,
            MaxHeight = _SplitterHeight,
            Background = new SolidColorBrush(KlavierTheme.Divider),
            IsVisible = false,
        };

        // Row definitions
        RowDefinition topRow = new() { Height = new GridLength(1, GridUnitType.Star), MinHeight = _MinHeight };
        _splitterRow = new RowDefinition { Height = GridLength.Auto };
        _settingsRow = new RowDefinition { Height = new GridLength(0), MinHeight = 0 };

        Grid grid = new()
        {
            RowDefinitions = { topRow, _splitterRow, _settingsRow },
        };

        Grid.SetRow(topSection, 0);
        Grid.SetRow(splitter, 1);
        Grid.SetRow(settingsPanel, 2);

        grid.Children.Add(topSection);
        grid.Children.Add(splitter);
        grid.Children.Add(settingsPanel);

        Content = grid;

        // Toggle settings
        toolbarView.SettingsToggled += isOpen =>
        {
            settingsPanel.IsVisible = isOpen;
            splitter.IsVisible = isOpen;

            if (isOpen)
            {
                _settingsRow.Height = new GridLength(_DefaultSettingsHeight);
                _settingsRow.MinHeight = _SettingsMinHeight;
                MinHeight = _MinHeight + _SettingsMinHeight + _SplitterHeight;
                Height += _DefaultSettingsHeight + _SplitterHeight;
            }
            else
            {
                double previousHeight = _settingsRow.Height.Value;
                _settingsRow.Height = new GridLength(0);
                _settingsRow.MinHeight = 0;
                MinHeight = _MinHeight;
                Height -= previousHeight + _SplitterHeight;
            }
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = _keyboardInput.HandleKeyDown(e.PhysicalKey, e.KeyModifiers);

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        e.Handled = _keyboardInput.HandleKeyUp(e.PhysicalKey);

        base.OnKeyUp(e);
    }
}
