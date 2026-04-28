using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.UI.Input;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Player;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private const string _WindowTitle = "Klavier";
    private const int _DefaultWidth = 1000;
    private const int _DefaultHeight = 280;
    private const int _MinWidth = 700;
    private const int _MinHeight = 150;
    private const int _SettingsMinHeight = 150;
    private const int _SplitterHeight = 8;
    private const int _DefaultSettingsHeight = 300;
    private const int _DefaultPlayerHeight = 250;
    private const int _PianoMinHeight = 100;
    private const int _PlayerMinHeight = 80;

    private readonly KeyboardInputHandler _keyboardInput;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        PlayerView playerView,
        ToolbarView toolbarView,
        SettingsPanel settingsPanel,
        MainWindowViewModel viewModel,
        DropOverlay dropOverlay)
    {
        _keyboardInput = keyboardInput;
        _viewModel = viewModel;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Topmost = viewModel.IsTopmost;
        Focusable = true;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsTopmost))
            {
                Topmost = viewModel.IsTopmost;
            }
        };

        // Piano section: piano (star) / separator / toolbar.
        Grid separator = CreatePianoSeparator();
        Grid pianoSection = new()
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = _PianoMinHeight },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
            },
        };
        Grid.SetRow(pianoView, 0);
        Grid.SetRow(separator, 1);
        Grid.SetRow(toolbarView, 2);
        pianoSection.Children.Add(pianoView);
        pianoSection.Children.Add(separator);
        pianoSection.Children.Add(toolbarView);

        // Player splitter straddles the player row's bottom edge - zero layout space, half-overflows into the piano section.
        DraggableSplitter playerSplitter = new(_SplitterHeight);
        playerSplitter.StraddleBottomBoundary();
        // Settings splitter sits in its own row, taking _SplitterHeight of layout space.
        DraggableSplitter settingsSplitter = new(_SplitterHeight);

        // Outer layout: player (collapsible, contains its straddling splitter) / pianoSection / settingsSplitter / settings (collapsible).
        RowDefinition playerRow = new();
        RowDefinition pianoSectionRow = new() { Height = new GridLength(1, GridUnitType.Star), MinHeight = _MinHeight };
        RowDefinition settingsSplitterRow = new() { Height = GridLength.Auto };
        RowDefinition settingsRow = new();

        Grid.SetRow(playerView, 0);
        Grid.SetRow(playerSplitter.HitArea, 0);
        Grid.SetRow(playerSplitter.Visual, 0);
        Grid.SetRow(pianoSection, 1);
        Grid.SetRow(settingsSplitter.HitArea, 2);
        Grid.SetRow(settingsSplitter.Visual, 2);
        Grid.SetRow(settingsPanel, 3);

        Grid mainGrid = new()
        {
            RowDefinitions = { playerRow, pianoSectionRow, settingsSplitterRow, settingsRow },
            Children =
            {
                playerView,
                playerSplitter.HitArea, playerSplitter.Visual,
                pianoSection,
                settingsSplitter.HitArea, settingsSplitter.Visual,
                settingsPanel,
            },
        };

        Content = new Panel { Children = { mainGrid, dropOverlay } };

        CollapsibleSection playerSection = new(
            content: playerView,
            splitter: playerSplitter,
            row: playerRow,
            window: this,
            defaultHeight: _DefaultPlayerHeight,
            minHeight: _PlayerMinHeight,
            splitterLayoutHeight: 0,
            growUpward: true);

        CollapsibleSection settingsSection = new(
            content: settingsPanel,
            splitter: settingsSplitter,
            row: settingsRow,
            window: this,
            defaultHeight: _DefaultSettingsHeight,
            minHeight: _SettingsMinHeight,
            splitterLayoutHeight: _SplitterHeight,
            growUpward: false,
            measureContentHeight: () =>
            {
                settingsPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return settingsPanel.DesiredSize.Height;
            });

        toolbarView.SettingsToggled += settingsSection.SetOpen;
        toolbarView.PlayerToggled += playerSection.SetOpen;

        // Blur any focused TextBox on a pointer click outside of it (commits the value via LostFocus).
        AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (e.Source is not TextBox)
            {
                Focus();
            }
        }, RoutingStrategies.Tunnel);

        // Drag-and-drop: window-wide, accepts only .mid/.midi/.sf2/.sf3.
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        string? path = TryFindSupportedFile(e);
        LoadableFileKind kind = path is null ? LoadableFileKind.Unsupported : LoadableFile.Classify(path);
        // Setting None tells the OS to show the no-drop cursor for unsupported file types.
        e.DragEffects = kind == LoadableFileKind.Unsupported ? DragDropEffects.None : DragDropEffects.Copy;
        _viewModel.OnDragOver(kind);
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        _viewModel.OnDragLeave();
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        await _viewModel.OnDropAsync(TryFindSupportedFile(e));
    }

    private static string? TryFindSupportedFile(DragEventArgs e)
    {
        IStorageItem[]? files = e.DataTransfer.TryGetFiles();
        if (files is null || files.Length > 1)
        {
            return null;
        }

        string path = files[0].Path.LocalPath;
        return LoadableFile.Classify(path) != LoadableFileKind.Unsupported ? path : null;
    }

    private static Grid CreatePianoSeparator()
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
        if (e.Source is not TextBox)
        {
            e.Handled = _keyboardInput.HandleKeyDown(e.PhysicalKey, e.KeyModifiers);
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Source is not TextBox)
        {
            e.Handled = _keyboardInput.HandleKeyUp(e.PhysicalKey);
        }

        base.OnKeyUp(e);
    }
}
