using CommunityToolkit.Mvvm.ComponentModel;
using Klavier.Core.Engine;
using Klavier.Core.Primitives;

namespace Klavier.UI.ViewModels;

public partial class PianoKeyViewModel(
    NotePitch pitch,
    bool isBlack,
    string keyLabel,
    string noteLabel,
    bool showKeyLabel,
    bool showNoteLabel,
    IPianoEngine pianoEngine)
    : ObservableObject
{
    public NotePitch Pitch { get; } = pitch;
    public bool IsBlack { get; } = isBlack;
    public string KeyLabel { get; } = keyLabel;

    [ObservableProperty]
    private bool _isPressed;

    [ObservableProperty]
    private string _noteLabel = noteLabel;

    [ObservableProperty]
    private bool _showKeyLabel = showKeyLabel;

    [ObservableProperty]
    private bool _showNoteLabel = showNoteLabel;

    public void Press()
    {
        pianoEngine.NoteOn(Pitch);
    }

    public void Release()
    {
        pianoEngine.NoteOff(Pitch);
    }
}
