using Klavier.Core.Engine;
using Klavier.Core.Events;
using Klavier.Core.Primitives;
using Klavier.Midi.Player;

namespace Klavier.Services;

public class MidiPlaybackCoordinator
{
    private readonly IMidiPlayer _player;
    private readonly IPianoEngine _engine;

    public MidiPlaybackCoordinator(IMidiPlayer player, IPianoEngine engine)
    {
        _player = player;
        _engine = engine;

        _player.NoteOn += OnPlayerNoteOn;
        _player.NoteOff += OnPlayerNoteOff;
        _player.SustainChanged += OnPlayerSustainChanged;
        _engine.PanicRaised += OnEnginePanicRaised;
    }

    private void OnPlayerNoteOn(NoteOnEvent noteEvent)
    {
        if (!_player.AudioEnabled)
        {
            return;
        }
        _engine.NoteOn(noteEvent.KeyPitch, noteEvent.Velocity);
    }

    private void OnPlayerNoteOff(NoteOffEvent noteEvent)
    {
        if (!_player.AudioEnabled)
        {
            return;
        }
        _engine.NoteOff(noteEvent.KeyPitch);
    }

    private void OnPlayerSustainChanged(bool isOn)
    {
        if (!_player.AudioEnabled)
        {
            return;
        }
        if (isOn)
        {
            _engine.SustainOn(InputSource.Playback);
        }
        else
        {
            _engine.SustainOff(InputSource.Playback);
        }
    }

    private void OnEnginePanicRaised()
    {
        _player.Pause();
    }
}
