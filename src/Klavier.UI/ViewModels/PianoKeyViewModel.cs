using CommunityToolkit.Mvvm.ComponentModel;
using Klavier.Core.Primitives;

namespace Klavier.UI.ViewModels;

public partial class PianoKeyViewModel(
    NotePitch pitch,
    bool isBlack,
    string keyLabel,
    string noteLabel)
    : ObservableObject
{
    public NotePitch Pitch { get; } = pitch;
    public bool IsBlack { get; } = isBlack;
    public string KeyLabel { get; } = keyLabel;

    [ObservableProperty]
    private bool _isPressed;

    [ObservableProperty]
    private string _noteLabel = noteLabel;
}
