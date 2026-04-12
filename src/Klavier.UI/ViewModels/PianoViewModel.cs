using System.Collections.Frozen;
using Avalonia.Threading;
using Klavier.Core.Engine;
using Klavier.Core.Events;
using Klavier.Config;
using Klavier.Core.Music;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Microsoft.Extensions.Options;
using Klavier.UI.Input.Mapping;

namespace Klavier.UI.ViewModels;

public class PianoViewModel : INoteEventHandler
{
    private const ushort _FirstPitch = 36;  // C2
    private const ushort _LastPitch = 96;   // C7

    private readonly FrozenDictionary<NotePitch, PianoKeyViewModel> _keysByPitch;
    private readonly IOptionsMonitor<UIConfig> _uiConfig;
    private readonly IOptionsMonitor<PianoConfig> _pianoConfig;

    public IReadOnlyList<PianoKeyViewModel> Keys { get; }
    public bool IsSustainOn { get; private set; }
    public event Action<bool>? SustainChanged;

    public PianoViewModel(IPianoEngine pianoEngine, IOptionsMonitor<UIConfig> uiConfig, IOptionsMonitor<PianoConfig> pianoConfig)
    {
        _uiConfig = uiConfig;
        _pianoConfig = pianoConfig;
        _uiConfig.OnChange(OnUIConfigChanged);
        _pianoConfig.OnChange(OnPianoConfigChanged);

        UIConfig config = _uiConfig.CurrentValue;
        Transpose transpose = new(_pianoConfig.CurrentValue.Transpose);
        FrozenDictionary<NotePitch, string> keyLabels = LoadKeyLabels(config.KeyboardLayout);

        List<PianoKeyViewModel> keys = [];

        for (ushort pitch = _FirstPitch; pitch <= _LastPitch; pitch++)
        {
            NotePitch keyPitch = new(pitch);
            NotePitch soundingPitch = keyPitch.Transpose(transpose);

            string keyLabel = keyLabels.TryGetValue(keyPitch, out string? label) ? label : "";
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
        Dispatcher.UIThread.Post(() =>
        {
            IsSustainOn = isOn;
            SustainChanged?.Invoke(isOn);
        });
    }

    private void OnUIConfigChanged(UIConfig newConfig)
    {
        Transpose transpose = new(_pianoConfig.CurrentValue.Transpose);
        FrozenDictionary<NotePitch, string> keyLabels = LoadKeyLabels(newConfig.KeyboardLayout);

        Dispatcher.UIThread.Post(() =>
        {
            foreach (PianoKeyViewModel key in Keys)
            {
                NotePitch soundingPitch = key.Pitch.Transpose(transpose);
                key.KeyLabel = keyLabels.TryGetValue(key.Pitch, out string? label) ? label : "";
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
                key.NoteLabel = NoteNames.GetNoteName(key.Pitch.Transpose(new Transpose(newConfig.Transpose)), noteNameStyle);
            }
        });
    }

    private static FrozenDictionary<NotePitch, string> LoadKeyLabels(string layoutName)
    {
        KeyboardMapping mapping = KeyboardMappingProvider.Load(layoutName);

        Dictionary<NotePitch, string> labels = [];

        foreach (KeyMappingEntry entry in mapping.WhiteKeys.Values)
        {
            labels[entry.Pitch] = entry.Label;
        }

        foreach (KeyMappingEntry entry in mapping.BlackKeys.Values)
        {
            labels[entry.Pitch] = entry.Label;
        }

        return labels.ToFrozenDictionary();
    }
}
