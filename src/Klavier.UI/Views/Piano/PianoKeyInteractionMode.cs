namespace Klavier.UI.Views.Piano;

public enum PianoKeyInteractionMode
{
    /// <summary>Click plays the note via the piano engine (default behavior).</summary>
    Play,

    /// <summary>Click raises <see cref="PianoKeyControl.KeyClicked"/> and does NOT play the note.</summary>
    Select,
}
