using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Klavier.UI.Input;
using Klavier.Config;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Toolbar;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private const string _WindowTitle = "Klavier";
    private const int _DefaultWidth = 1000;
    private const int _DefaultHeight = 300;
    private const int _MinWidth = 700;
    private const int _MinHeight = 150;
    private const int _SettingsMinHeight = 150;
    private const int _SplitterHeight = 8;
    private const int _DefaultSettingsHeight = 200;

    private readonly KeyboardInputHandler _keyboardInput;
    private readonly SettingsPanel _settingsPanel;
    private readonly DraggableSplitter _splitter;
    private readonly RowDefinition _settingsRow;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        ToolbarView toolbarView,
        SettingsPanel settingsPanel,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _keyboardInput = keyboardInput;
        _settingsPanel = settingsPanel;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Topmost = uiConfig.CurrentValue.Topmost;

        uiConfig.OnChange(config => Avalonia.Threading.Dispatcher.UIThread.Post(() => Topmost = config.Topmost));

        // Top section: piano + toolbar
        Grid separator = CreateCenteredSeparator();
        DockPanel.SetDock(toolbarView, Dock.Bottom);
        DockPanel.SetDock(separator, Dock.Bottom);

        DockPanel topSection = new()
        {
            Children = { toolbarView, separator, pianoView },
        };

        // Draggable splitter between top section and settings panel
        _splitter = new DraggableSplitter(_SplitterHeight);

        // Layout: top section + splitter + settings panel
        RowDefinition topRow = new() { Height = new GridLength(1, GridUnitType.Star), MinHeight = _MinHeight };
        RowDefinition splitterRow = new() { Height = GridLength.Auto };
        _settingsRow = new RowDefinition { Height = new GridLength(0), MinHeight = 0 };

        Grid.SetRow(topSection, 0);
        Grid.SetRow(_splitter.HitArea, 1);
        Grid.SetRow(_splitter.Visual, 1);
        Grid.SetRow(_settingsPanel, 2);

        Content = new Grid
        {
            RowDefinitions = { topRow, splitterRow, _settingsRow },
            Children = { topSection, _splitter.HitArea, _splitter.Visual, _settingsPanel },
        };

        // Toggle settings
        toolbarView.SettingsToggled += ToggleSettingsPanel;
    }

    private void ToggleSettingsPanel(bool isOpen)
    {
        _settingsPanel.IsVisible = isOpen;
        _splitter.IsVisible = isOpen;

        if (isOpen)
        {
            _settingsPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double contentHeight = _settingsPanel.DesiredSize.Height;

            _settingsRow.Height = new GridLength(Math.Min(_DefaultSettingsHeight, contentHeight));
            _settingsRow.MinHeight = _SettingsMinHeight;
            _settingsRow.MaxHeight = contentHeight;
            MinHeight = _MinHeight + _SettingsMinHeight + _SplitterHeight;
            Height += _settingsRow.Height.Value + _SplitterHeight;
        }
        else
        {
            double previousHeight = _settingsRow.Height.Value;
            _settingsRow.Height = new GridLength(0);
            _settingsRow.MinHeight = 0;
            _settingsRow.MaxHeight = double.PositiveInfinity;
            MinHeight = _MinHeight;
            Height -= previousHeight + _SplitterHeight;
        }
    }

    private static Grid CreateCenteredSeparator()
    {
        Border line = new()
        {
            Height = 1,
            Background = new SolidColorBrush(ThemePaletteProvider.Divider),
        };

        Grid grid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
        };

        Grid.SetColumn(line, 1);
        grid.Children.Add(line);

        return grid;
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
