using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.UI.Input;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Layout;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Player;
using Klavier.UI.Views.Settings;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private const string _WindowTitle = "Klavier";
    private const int _DefaultWidth = 1000;
    private const int _DefaultHeight = 280;
    private const int _MinWidth = 700;
    private const int _MinHeight = 150;

    private readonly KeyboardInputHandler _keyboardInput;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(
        KeyboardInputHandler keyboardInput,
        PianoView pianoView,
        PlayerView playerView,
        ToolbarView toolbarView,
        SettingsView settingsPanel,
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

        MainWindowLayout layout = new(this, pianoView, playerView, toolbarView, settingsPanel, dropOverlay);
        Content = layout.Root;

        toolbarView.SettingsToggled += layout.SettingsSection.SetOpen;
        toolbarView.PlayerToggled += layout.PlayerSection.SetOpen;

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
