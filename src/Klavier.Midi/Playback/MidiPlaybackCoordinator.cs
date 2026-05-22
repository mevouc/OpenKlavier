using Klavier.Core.Engine;
using Klavier.Core.Primitives;

namespace Klavier.Midi.Playback;

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
        _player.AudioEnabledChanged += OnAudioEnabledChanged;
        _player.Loaded += OnPlayerLoaded;
        _player.Stopped += DrainPlaybackState;
        _player.Finished += DrainPlaybackState;
        _engine.PanicRaised += OnEnginePanicRaised;
    }

    private void OnPlayerNoteOn(PlaybackNoteOn noteEvent)
    {
        if (!_player.AudioEnabled)
        {
            return;
        }
        _engine.NoteOn(noteEvent.Pitch, noteEvent.Velocity, InputSource.Playback);
    }

    private void OnPlayerNoteOff(PlaybackNoteOff noteEvent)
    {
        if (!_player.AudioEnabled)
        {
            return;
        }
        _engine.NoteOff(noteEvent.Pitch, InputSource.Playback);
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

    private void OnAudioEnabledChanged(bool enabled)
    {
        if (enabled)
        {
            return;
        }
        DrainPlaybackState();
    }

    private void OnPlayerLoaded(MidiScore _)
    {
        DrainPlaybackState();
    }

    private void DrainPlaybackState()
    {
        _engine.AllNotesOff(InputSource.Playback);
        _engine.SustainOff(InputSource.Playback);
    }

    private void OnEnginePanicRaised()
    {
        _player.Pause();
    }
}
