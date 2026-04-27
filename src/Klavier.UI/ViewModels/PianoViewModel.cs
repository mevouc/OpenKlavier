using System.Collections.Frozen;
using Avalonia.Threading;
using Klavier.Core.Engine;
using Klavier.Core.Events;
using Klavier.Core.Music;
using Klavier.Core.Ports;
using Klavier.Core.Primitives;
using Microsoft.Extensions.Options;
using Klavier.UI.Input.Mapping;
using Klavier.Config.Schema;

namespace Klavier.UI.ViewModels;

public class PianoViewModel : INoteEventHandler
{
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
        IReadOnlyDictionary<NotePitch, string> keyLabels = LoadKeyLabels(config.KeyboardLayout);

        List<PianoKeyViewModel> keys = PianoKeysBuilder.Build(
            pianoEngine, keyLabels, config.NoteNameStyle, transpose,
            config.ShowKeyLabels, config.ShowNoteLabels);

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
        IReadOnlyDictionary<NotePitch, string> keyLabels = LoadKeyLabels(newConfig.KeyboardLayout);

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

    private static IReadOnlyDictionary<NotePitch, string> LoadKeyLabels(string layoutName)
    {
        return KeyboardMappingProvider.Load(layoutName).ToLabelsByPitch();
    }
}
