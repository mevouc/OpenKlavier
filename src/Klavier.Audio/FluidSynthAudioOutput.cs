using Klavier.Config;
using Klavier.Core.Events;
using Klavier.Core.Ports;
using Klavier.SoundFont;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFluidsynth;

namespace Klavier.Audio;

public class FluidSynthAudioOutput : IAudioOutput, ISoundFontInfoProvider
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
    private uint _sfontId;
    private SoundFontInfo _info = new(null, new Dictionary<(int Bank, int Program), SoundFontPreset>());

    private bool _isDisposed;

    public event Action? SoundFontInfoChanged;

    public SoundFontInfo GetSoundFontInfo() => _info;

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
                const string fluidSynthInternalLoggerMessage = "FluidSynth ({Level}): {Message}";

                switch (level)
                {
                    case Logger.LogLevel.Panic:
                    case Logger.LogLevel.Error:
                        _logger.LogError(fluidSynthInternalLoggerMessage, level, message);
                        break;
                    case Logger.LogLevel.Warning:
                        _logger.LogWarning(fluidSynthInternalLoggerMessage, level, message);
                        break;
                    case Logger.LogLevel.Information:
                        _logger.LogInformation(fluidSynthInternalLoggerMessage, level, message);
                        break;
                    default:
                        _logger.LogDebug(fluidSynthInternalLoggerMessage, level, message);
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
        LoadSoundFontAndApplyPreset(_audioConfig.CurrentValue.SoundFont);

        _audioDriver = new(_synthSettings, _synth);
    }

    private void LoadSoundFontAndApplyPreset(SoundFontConfig soundFontConfig)
    {
        if (_synth is null)
        {
            _logger.LogError("Cannot load SoundFont: Synth not initialized");
            return;
        }
        try
        {
            _sfontId = _synth.LoadSoundFont(soundFontConfig.Path, true);
            _info = SoundFontParser.ParseInfo(soundFontConfig.Path);
        }
        catch (InvalidDataException e)
        {
            _logger.LogError(e, "Error parsing SoundFont: {Message}", e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFluidsynth LoadSoundFont exception: {Message}", e.Message);
        }
        ApplyPreset(soundFontConfig.Preset);
    }

    private void ApplyPreset(SoundFontPresetConfig presetConfig)
    {
        if (!_info.Presets.ContainsKey((presetConfig.Bank, presetConfig.Program)))
        {
            _logger.LogError("SoundFont has no preset at bank {Bank} program {Program}", presetConfig.Bank, presetConfig.Program);
            return;
        }
        try
        {
            _synth?.ProgramSelect(_MidiChannel, _sfontId, (uint)presetConfig.Bank, (uint)presetConfig.Program);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFluidSynth ProgramSelect exception: {Message}", e.Message);
        }
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

        if (newConfig.SoundFont.Path != _lastAudioConfig.SoundFont.Path)
        {
            _synth?.UnloadSoundFont(_sfontId, true);
            LoadSoundFontAndApplyPreset(newConfig.SoundFont);
            SoundFontInfoChanged?.Invoke();
        }
        else if (newConfig.SoundFont.Preset.Bank != _lastAudioConfig.SoundFont.Preset.Bank
            || newConfig.SoundFont.Preset.Program != _lastAudioConfig.SoundFont.Preset.Program)
        {
            ApplyPreset(newConfig.SoundFont.Preset);
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
