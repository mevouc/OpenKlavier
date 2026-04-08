using System.Collections.Frozen;
using Avalonia.Threading;
using Klavier.Core.Engine;
using Klavier.Core.Events;
using Klavier.Core.Music;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Klavier.Core.Options;
using Klavier.UI.Options;
using Microsoft.Extensions.Options;

namespace Klavier.UI.ViewModels;

public class PianoViewModel : INoteEventHandler
{
    private const ushort _FirstPitch = 36;  // C2
    private const ushort _LastPitch = 96;   // C7

    // QWERTY key labels
    private static readonly string[] _QwertyDigitRow = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
    private static readonly string[] _QwertyTopRow = ["Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"];
    private static readonly string[] _QwertyHomeRow = ["A", "S", "D", "F", "G", "H", "J", "K", "L"];
    private static readonly string[] _QwertyBottomRow = ["Z", "X", "C", "V", "B", "N", "M"];

    // AZERTY key labels
    private static readonly string[] _AzertyDigitRow = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
    private static readonly string[] _AzertyTopRow = ["A", "Z", "E", "R", "T", "Y", "U", "I", "O", "P"];
    private static readonly string[] _AzertyHomeRow = ["Q", "S", "D", "F", "G", "H", "J", "K", "L", "M"];
    private static readonly string[] _AzertyBottomRow = ["W", "X", "C", "V", "B", "N"];

    private readonly FrozenDictionary<NotePitch, PianoKeyViewModel> _keysByPitch;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly IOptionsMonitor<PianoConfig> _pianoConfig;

    public IReadOnlyList<PianoKeyViewModel> Keys { get; }

    public PianoViewModel(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig, IOptionsMonitor<PianoConfig> pianoConfig)
    {
        _uiConfig = uiConfig;
        _pianoConfig = pianoConfig;
        _uiConfig.OnChange(OnUIConfigChanged);
        _pianoConfig.OnChange(OnPianoConfigChanged);

        UIConfig config = _uiConfig.CurrentValue;
        short transpose = _pianoConfig.CurrentValue.Transpose;
        Dictionary<ushort, string> keyLabels = BuildKeyLabels(config.KeyboardLayout);

        List<PianoKeyViewModel> keys = [];

        for (ushort pitch = _FirstPitch; pitch <= _LastPitch; pitch++)
        {
            NotePitch keyPitch = new(pitch);
            NotePitch soundingPitch = keyPitch.Transpose(transpose);

            string keyLabel = keyLabels.TryGetValue(pitch, out string? label) ? label : "";
            string noteLabel = NoteNames.GetNoteName(soundingPitch, config.NoteNameStyle);

            keys.Add(new PianoKeyViewModel(
                keyPitch, keyLabel, noteLabel,
                config.ShowKeyLabels, config.ShowNoteLabels, pianoEngine));
        }

        Keys = keys;
        _keysByPitch = keys.ToFrozenDictionary(k => k.Pitch);
    }

    public void OnNoteOn(NoteOnEvent noteOnEvent)
    {
        if (_keysByPitch.TryGetValue(noteOnEvent.KeyPitch, out PianoKeyViewModel? key))
        {
            Dispatcher.UIThread.Post(() => key.IsPressed = true);
        }
    }

    public void OnNoteOff(NoteOffEvent noteOffEvent)
    {
        if (_keysByPitch.TryGetValue(noteOffEvent.KeyPitch, out PianoKeyViewModel? key))
        {
            Dispatcher.UIThread.Post(() => key.IsPressed = false);
        }
    }

    public void OnSustainChanged(bool isOn)
    {
        // UI doesn't visually reflect sustain state for now
    }

    private void OnUIConfigChanged(UIConfig newConfig)
    {
        short transpose = _pianoConfig.CurrentValue.Transpose;
        Dictionary<ushort, string> keyLabels = BuildKeyLabels(newConfig.KeyboardLayout);

        Dispatcher.UIThread.Post(() =>
        {
            foreach (PianoKeyViewModel key in Keys)
            {
                NotePitch soundingPitch = key.Pitch.Transpose(transpose);
                key.KeyLabel = keyLabels.TryGetValue(key.Pitch.Value, out string? label) ? label : "";
                key.NoteLabel = NoteNames.GetNoteName(soundingPitch, newConfig.NoteNameStyle);
                key.ShowKeyLabel = newConfig.ShowKeyLabels;
                key.ShowNoteLabel = newConfig.ShowNoteLabels;
            }
        });
    }

    private void OnPianoConfigChanged(PianoConfig newConfig)
    {
        NoteNameStyle noteNameStyle = _uiConfig.CurrentValue.NoteNameStyle;

        Dispatcher.UIThread.Post(() =>
        {
            foreach (PianoKeyViewModel key in Keys)
            {
                key.NoteLabel = NoteNames.GetNoteName(key.Pitch.Transpose(newConfig.Transpose), noteNameStyle);
            }
        });
    }

    private static Dictionary<ushort, string> BuildKeyLabels(KeyboardLayout layout)
    {
        bool isAzerty = layout == KeyboardLayout.AZERTY;

        Dictionary<ushort, string> labels = [];
        ushort pitch = _FirstPitch;

        AssignRowLabels(labels, isAzerty ? _AzertyDigitRow : _QwertyDigitRow, ref pitch);
        AssignRowLabels(labels, isAzerty ? _AzertyTopRow : _QwertyTopRow, ref pitch);
        AssignRowLabels(labels, isAzerty ? _AzertyHomeRow : _QwertyHomeRow, ref pitch);
        AssignRowLabels(labels, isAzerty ? _AzertyBottomRow : _QwertyBottomRow, ref pitch);

        return labels;
    }

    private static void AssignRowLabels(Dictionary<ushort, string> labels, string[] row, ref ushort pitch)
    {
        int rowIndex = 0;

        while (rowIndex < row.Length && pitch <= _LastPitch)
        {
            NotePitch current = new(pitch);

            if (current.IsAccidental)
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
                    NotePitch next = new(pitch);

                    if (next.IsAccidental)
                    {
                        labels[pitch] = $"⇧{row[rowIndex - 1]}";
                        pitch++;
                    }
                }
            }
        }
    }
}
