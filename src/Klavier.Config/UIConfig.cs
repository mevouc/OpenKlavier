namespace Klavier.Config;

public class UIConfig
{
    public const string SectionName = "UI";

    public bool Topmost { get; init; }
    public bool ShowKeyLabels { get; init; } = true;
    public bool ShowNoteLabels { get; init; } = true;
    public NoteNameStyle NoteNameStyle { get; init; } = NoteNameStyle.Scientific;
    public SustainMode SustainMode { get; init; } = SustainMode.Hold;
    public string KeyboardLayout { get; init; } = "qwerty";
}
