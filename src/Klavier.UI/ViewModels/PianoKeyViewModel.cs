using CommunityToolkit.Mvvm.ComponentModel;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;

namespace Klavier.UI.ViewModels;

public partial class PianoKeyViewModel(
    NotePitch pitch,
    string keyLabel,
    string noteLabel,
    bool showKeyLabel,
    bool showNoteLabel,
    IPianoEngine pianoEngine)
    : ObservableObject
{
    public NotePitch Pitch { get; } = pitch;
    public bool IsBlack => Pitch.IsAccidental;

    [ObservableProperty]
    public partial string KeyLabel { get; set; } = keyLabel;

    [ObservableProperty]
    public partial bool IsPressed { get; set; }

    [ObservableProperty]
    public partial string NoteLabel { get; set; } = noteLabel;

    [ObservableProperty]
    public partial bool ShowKeyLabel { get; set; } = showKeyLabel;

    [ObservableProperty]
    public partial bool ShowNoteLabel { get; set; } = showNoteLabel;

    public void Press() => pianoEngine.NoteOn(Pitch);

    public void Release() => pianoEngine.NoteOff(Pitch);
}
