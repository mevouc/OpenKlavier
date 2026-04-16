using Klavier.Config;
using Klavier.Core.Events;
using Klavier.Core.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFluidsynth;

namespace Klavier.Audio;

public class FluidSynthAudioOutput : IAudioOutput
{
    private const int _MidiChannel = 0;
    private const int _SustainController = 64; // MIDI CC64
    private const int _ControllerOn = 127;
    private const int _ControllerOff = 0;
    private readonly Settings _synthSettings;
    private readonly IOptionsMonitor<AudioConfig> _audioConfig;
    private readonly ILogger<FluidSynthAudioOutput> _logger;
    private AudioConfig _lastAudioConfig;
    private Synth? _synth;
    private AudioDriver? _audioDriver;

    private bool _isDisposed;

    public FluidSynthAudioOutput(
        IOptionsMonitor<AudioConfig> audioConfig,
        ILogger<FluidSynthAudioOutput> logger)
    {
        _audioConfig = audioConfig;
        _logger = logger;

        _lastAudioConfig = _audioConfig.CurrentValue;
        _audioConfig.OnChange(OnAudioConfigChanged); // dynamically update volume/gain

        ConfigureThirdPartyLogging();
        _synthSettings = new Settings();
    }

    private void ConfigureThirdPartyLogging()
    {
        Logger.LogLevel minimumLogLevel = Enum.Parse<Logger.LogLevel>(_audioConfig.CurrentValue.MinimumFluidSynthLogLevel);

        Logger.SetLoggerMethod((level, message, _) =>
        {
            if (level <= minimumLogLevel)
            {
                switch (level)
                {
                    case Logger.LogLevel.Panic:
                    case Logger.LogLevel.Error:
                        _logger.LogError("FluidSynth ({Level}): {Message}", level, message);
                        break;
                    case Logger.LogLevel.Warning:
                        _logger.LogWarning("FluidSynth ({Level}): {Message}", level, message);
                        break;
                    case Logger.LogLevel.Information:
                        _logger.LogInformation("FluidSynth ({Level}): {Message}", level, message);
                        break;
                    default:
                        _logger.LogDebug("FluidSynth ({Level}): {Message}", level, message);
                        break;
                }
            }
        });
    }

    public void Initialize()
    {
        _synthSettings[ConfigurationKeys.AudioDriver].StringValue = _audioConfig.CurrentValue.AudioDriver;
        _synthSettings[ConfigurationKeys.SynthGain].DoubleValue = _audioConfig.CurrentValue.GainFactor;

        _synth = new(_synthSettings);
        _synth.LoadSoundFont(_audioConfig.CurrentValue.SoundFont.Path, true);

        _audioDriver = new(_synthSettings, _synth);
    }

    public void OnNoteOn(NoteOnEvent noteOnEvent)
    {
        try
        {
            _synth?.NoteOn(_MidiChannel, noteOnEvent.SoundingPitch.Value, noteOnEvent.Velocity.Value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFluidSynth NoteOn exception: {Message}", e.Message);
        }
    }

    public void OnNoteOff(NoteOffEvent noteOffEvent)
    {
        try
        {
            _synth?.NoteOff(_MidiChannel, noteOffEvent.SoundingPitch.Value);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "NFluidSynth NoteOff exception: {Message}", e.Message);
        }
    }

    public void OnSustainChanged(bool isOn)
    {
        try
        {
            _synth?.CC(_MidiChannel, _SustainController, isOn ? _ControllerOn : _ControllerOff);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFluidSynth CC exception: {Message}", e.Message);
        }
    }

    private void OnAudioConfigChanged(AudioConfig newConfig)
    {
        if (newConfig.VolumeInPercent != _lastAudioConfig.VolumeInPercent)
        {
            _synth?.Gain = newConfig.GainFactor;
        }
        _lastAudioConfig = newConfig;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _audioDriver?.Dispose();
                _synth?.Dispose();
                _synthSettings.Dispose();
            }

            _audioDriver = null;
            _synth = null;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
