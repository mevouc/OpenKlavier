using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.UI.Input;
using Klavier.Config.Schema;
using Klavier.Midi.Loading;
using Klavier.SoundFont.Loading;
using Klavier.UI.Theme;
using Microsoft.Extensions.Options;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Player;
using Avalonia.Controls.Shapes;
using Path = System.IO.Path;

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
    private const string _DropMidiLabel = "Load this MIDI file";
    private const string _DropSoundFontLabel = "Load this SoundFont file";

    private readonly KeyboardInputHandler _keyboardInput;
    private readonly IMidiFileLoader _midiFileLoader;
    private readonly ISoundFontFileLoader _soundFontFileLoader;
    private readonly Border _dropOverlay;
    private readonly TextBlock _dropOverlayLabel;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        PlayerView playerView,
        ToolbarView toolbarView,
        SettingsPanel settingsPanel,
        IMidiFileLoader midiFileLoader,
        ISoundFontFileLoader soundFontFileLoader,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _keyboardInput = keyboardInput;
        _midiFileLoader = midiFileLoader;
        _soundFontFileLoader = soundFontFileLoader;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
        Topmost = uiConfig.CurrentValue.Topmost;
        Focusable = true;

        uiConfig.OnChange(config => Avalonia.Threading.Dispatcher.UIThread.Post(() => Topmost = config.Topmost));

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

        // Drop overlay: translucent backdrop with a dashed accent frame and a centered label, hidden by default.
        // IsHitTestVisible=false lets drag events pass through to the window's DragOver/Drop handlers.
        _dropOverlayLabel = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(ThemePaletteProvider.TextPrimary),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Rectangle dashedFrame = new()
        {
            Stroke = new SolidColorBrush(ThemePaletteProvider.TextPrimary),
            StrokeThickness = 4,
            StrokeDashArray = [6, 4],
            Margin = new Thickness(2),
        };
        _dropOverlay = new Border
        {
            Background = new SolidColorBrush(ThemePaletteProvider.AppBackground) { Opacity = 0.7 },
            IsHitTestVisible = false,
            IsVisible = false,
            Child = new Grid { Children = { dashedFrame, _dropOverlayLabel } },
        };

        Content = new Panel { Children = { mainGrid, _dropOverlay } };

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
        // Setting None tells the OS to show the no-drop cursor for unsupported file types.
        e.DragEffects = path is not null ? DragDropEffects.Copy : DragDropEffects.None;
        if (path is not null)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            _dropOverlayLabel.Text = ext is ".mid" or ".midi" ? _DropMidiLabel : _DropSoundFontLabel;
            _dropOverlay.IsVisible = true;
        }
        else
        {
            _dropOverlay.IsVisible = false;
        }
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        _dropOverlay.IsVisible = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        _dropOverlay.IsVisible = false;
        string? path = TryFindSupportedFile(e);
        if (path is null)
        {
            return;
        }
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".mid" or ".midi")
        {
            await _midiFileLoader.TryLoadAsync(path);
        }
        else if (ext is ".sf2" or ".sf3")
        {
            await _soundFontFileLoader.TryLoadAsync(path);
        }
    }

    private static string? TryFindSupportedFile(DragEventArgs e)
    {
        IStorageItem[]? files = e.DataTransfer.TryGetFiles();
        if (files is null || files.Length > 1)
        {
            return null;
        }

        string path = files[0].Path.LocalPath;
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".mid" or ".midi" or ".sf2" or ".sf3")
        {
            return path;
        }
        return null;
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
