using Klavier.Core.Music;

namespace Klavier.UI.Options;

public class UIConfig
{
    public bool Topmost { get; init; }
    public bool ShowKeyLabels { get; init; } = true;
    public bool ShowNoteLabels { get; init; } = true;
    public NoteNameStyle NoteNameStyle { get; init; } = NoteNameStyle.Scientific;
    public SustainMode SustainMode { get; init; } = SustainMode.Hold;
}
