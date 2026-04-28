using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Klavier.Config.Schema;
using Klavier.Midi.Loading;
using Klavier.SoundFont.Loading;
using Microsoft.Extensions.Options;

namespace Klavier.UI.ViewModels;

public enum LoadableFileKind
{
    Unsupported,
    Midi,
    SoundFont,
}

public static class LoadableFile
{
    public static LoadableFileKind Classify(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".mid" or ".midi" => LoadableFileKind.Midi,
            ".sf2" or ".sf3" => LoadableFileKind.SoundFont,
            _ => LoadableFileKind.Unsupported,
        };
    }
}

public partial class MainWindowViewModel : ObservableObject
{
    private const string _DropMidiLabel = "Load this MIDI file";
    private const string _DropSoundFontLabel = "Load this SoundFont file";

    private readonly IMidiFileLoader _midiFileLoader;
    private readonly ISoundFontFileLoader _soundFontFileLoader;

    [ObservableProperty]
    public partial bool IsDropOverlayVisible { get; set; }

    [ObservableProperty]
    public partial string DropOverlayLabel { get; set; } = "";

    [ObservableProperty]
    public partial bool IsTopmost { get; set; }

    public MainWindowViewModel(
        IMidiFileLoader midiFileLoader,
        ISoundFontFileLoader soundFontFileLoader,
        IOptionsMonitor<UIConfig> uiConfig)
    {
        _midiFileLoader = midiFileLoader;
        _soundFontFileLoader = soundFontFileLoader;

        IsTopmost = uiConfig.CurrentValue.Topmost;
        uiConfig.OnChange(config => Dispatcher.UIThread.Post(() => IsTopmost = config.Topmost));
    }

    public void OnDragOver(LoadableFileKind kind)
    {
        if (kind == LoadableFileKind.Unsupported)
        {
            IsDropOverlayVisible = false;
            return;
        }
        DropOverlayLabel = kind switch
        {
            LoadableFileKind.Midi => _DropMidiLabel,
            LoadableFileKind.SoundFont => _DropSoundFontLabel,
            _ => "",
        };
        IsDropOverlayVisible = true;
    }

    public void OnDragLeave() => IsDropOverlayVisible = false;

    // Hides the overlay immediately, before awaiting the loader, so the user doesn't see a stale
    // overlay while a large file is parsing.
    public async Task OnDropAsync(string? path)
    {
        IsDropOverlayVisible = false;
        if (path is null)
        {
            return;
        }
        switch (LoadableFile.Classify(path))
        {
            case LoadableFileKind.Midi:
                await _midiFileLoader.TryLoadAsync(path);
                break;
            case LoadableFileKind.SoundFont:
                await _soundFontFileLoader.TryLoadAsync(path);
                break;
        }
    }
}
