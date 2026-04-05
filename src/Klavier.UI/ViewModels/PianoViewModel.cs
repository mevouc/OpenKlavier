using System.Collections.Frozen;
using Avalonia.Threading;
using Klavier.Core.Events;
using Klavier.Core.Music;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Klavier.UI.Options;
using Microsoft.Extensions.Options;

namespace Klavier.UI.ViewModels;

public class PianoViewModel : INoteEventHandler
{
    private const int _FirstPitch = 36;  // C2
    private const int _LastPitch = 96;   // C7

    private static readonly int[] _BlackNoteIndices = [1, 3, 6, 8, 10];

    // QWERTY key labels: white key label → Shift+label for the sharp above
    private static readonly string[] _DigitRow = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
    private static readonly string[] _TopRow = ["Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"];
    private static readonly string[] _HomeRow = ["A", "S", "D", "F", "G", "H", "J", "K", "L"];
    private static readonly string[] _BottomRow = ["Z", "X", "C", "V", "B", "N", "M"];

    private readonly FrozenDictionary<NotePitch, PianoKeyViewModel> _keysByPitch;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;

    public IReadOnlyList<PianoKeyViewModel> Keys { get; }

    public PianoViewModel(IOptionsMonitor<UIConfig> uiConfig)
    {
        _uiConfig = uiConfig;
        _uiConfig.OnChange(OnUIConfigChanged);

        NoteNameStyle style = _uiConfig.CurrentValue.NoteNameStyle;
        Dictionary<int, string> keyLabels = BuildKeyLabels();

        List<PianoKeyViewModel> keys = [];

        for (int pitch = _FirstPitch; pitch <= _LastPitch; pitch++)
        {
            int noteIndex = pitch % 12;
            bool isBlack = _BlackNoteIndices.Contains(noteIndex);
            NotePitch notePitch = new((ushort)pitch);
            string keyLabel = keyLabels.TryGetValue(pitch, out string? label) ? label : "";
            string noteLabel = NoteNames.GetNoteName(notePitch, style);

            keys.Add(new PianoKeyViewModel(notePitch, isBlack, keyLabel, noteLabel));
        }

        Keys = keys;
        _keysByPitch = keys.ToFrozenDictionary(k => k.Pitch);
    }

    public void OnNoteOn(NoteOnEvent noteOnEvent)
    {
        if (_keysByPitch.TryGetValue(noteOnEvent.Pitch, out PianoKeyViewModel? key))
        {
            Dispatcher.UIThread.Post(() => key.IsPressed = true);
        }
    }

    public void OnNoteOff(NoteOffEvent noteOffEvent)
    {
        if (_keysByPitch.TryGetValue(noteOffEvent.Pitch, out PianoKeyViewModel? key))
        {
            Dispatcher.UIThread.Post(() => key.IsPressed = false);
        }
    }

    private void OnUIConfigChanged(UIConfig newConfig)
    {
        NoteNameStyle style = newConfig.NoteNameStyle;

        Dispatcher.UIThread.Post(() =>
        {
            foreach (PianoKeyViewModel key in Keys)
            {
                key.NoteLabel = NoteNames.GetNoteName(key.Pitch, style);
            }
        });
    }

    private static Dictionary<int, string> BuildKeyLabels()
    {
        Dictionary<int, string> labels = [];
        int pitch = _FirstPitch;

        AssignRowLabels(labels, _DigitRow, ref pitch);
        AssignRowLabels(labels, _TopRow, ref pitch);
        AssignRowLabels(labels, _HomeRow, ref pitch);
        AssignRowLabels(labels, _BottomRow, ref pitch);

        return labels;
    }

    private static void AssignRowLabels(Dictionary<int, string> labels, string[] row, ref int pitch)
    {
        int rowIndex = 0;

        while (rowIndex < row.Length && pitch <= _LastPitch)
        {
            int noteIndex = pitch % 12;
            bool isBlack = _BlackNoteIndices.Contains(noteIndex);

            if (isBlack)
            {
                // Black key gets Shift+previous white key label
                labels[pitch] = $"⇧{row[rowIndex - 1]}";
                pitch++;
            }
            else
            {
                // White key gets the next label in the row
                labels[pitch] = row[rowIndex];
                rowIndex++;
                pitch++;

                // Check if next note is a black key — assign its label too
                if (pitch <= _LastPitch)
                {
                    int nextNoteIndex = pitch % 12;
                    bool nextIsBlack = _BlackNoteIndices.Contains(nextNoteIndex);

                    if (nextIsBlack)
                    {
                        labels[pitch] = $"⇧{row[rowIndex - 1]}";
                        pitch++;
                    }
                }
            }
        }
    }
}
