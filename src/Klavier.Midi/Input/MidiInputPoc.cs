using Klavier.Core.Engine;
using Klavier.Core.Primitives;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Extensions.Logging;

namespace Klavier.Midi.Input;

// POC for iteration 15 / Step 0. Whole class deleted in Step 4 once IMidiInputDevice + DryWetMidiInputDevice exist.
public sealed class MidiInputPoc : IDisposable
{
    private const int _SustainController = 64;
    private const int _SustainOnThreshold = 64;

    private readonly IPianoEngine _engine;
    private readonly InputDevice? _device;

    public MidiInputPoc(IPianoEngine engine, ILogger<MidiInputPoc> logger)
    {
        _engine = engine;

        ICollection<InputDevice> discovered = InputDevice.GetAll();
        try
        {
            if (discovered.Count == 0)
            {
                logger.LogWarning("No MIDI input devices found");
                return;
            }

            InputDevice device = discovered.First();
            device.EventReceived += OnEventReceived;
            device.StartEventsListening();
            _device = device;

            logger.LogInformation("POC opened MIDI input device {Name}", device.Name);
        }
        finally
        {
            foreach (InputDevice d in discovered
                .Where(d => !ReferenceEquals(d, _device)))
            {
                d.Dispose();
            }
        }
    }

    private void OnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        switch (e.Event)
        {
            case NoteOnEvent noteOn:
                _engine.NoteOn(
                    new NotePitch((byte)noteOn.NoteNumber),
                    new NoteVelocity((byte)noteOn.Velocity),
                    InputSource.MidiDevice);
                break;
            case NoteOffEvent noteOff:
                _engine.NoteOff(
                    new NotePitch((byte)noteOff.NoteNumber),
                    InputSource.MidiDevice);
                break;
            case ControlChangeEvent cc when (byte)cc.ControlNumber == _SustainController:
                if ((byte)cc.ControlValue >= _SustainOnThreshold)
                {
                    _engine.SustainOn(InputSource.MidiDevice);
                }
                else
                {
                    _engine.SustainOff(InputSource.MidiDevice);
                }
                break;
        }
    }

    public void Dispose()
    {
        if (_device is null)
        {
            return;
        }
        _device.StopEventsListening();
        _device.EventReceived -= OnEventReceived;
        _device.Dispose();
    }
}
