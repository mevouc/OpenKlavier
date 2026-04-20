using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.Core.Music;
using Klavier.Core.Primitives;

namespace Klavier.UI.ViewModels;

public static class PianoKeysBuilder
{
    public const ushort FirstPitch = 36;   // C2
    public const ushort LastPitch = 96;    // C7

    public static List<PianoKeyViewModel> Build(
        IPianoEngine pianoEngine,
        IReadOnlyDictionary<NotePitch, string> keyLabels,
        NoteNameStyle noteNameStyle,
        Transpose transpose,
        bool showKeyLabels,
        bool showNoteLabels)
    {
        List<PianoKeyViewModel> keys = [];

        for (ushort pitch = FirstPitch; pitch <= LastPitch; pitch++)
        {
            NotePitch keyPitch = new(pitch);
            NotePitch soundingPitch = keyPitch.Transpose(transpose);

            string keyLabel = keyLabels.TryGetValue(keyPitch, out string? label) ? label : "";
            string noteLabel = NoteNames.GetNoteName(soundingPitch, noteNameStyle);

            keys.Add(new PianoKeyViewModel(
                keyPitch, keyLabel, noteLabel,
                showKeyLabels, showNoteLabels, pianoEngine));
        }

        return keys;
    }
}
