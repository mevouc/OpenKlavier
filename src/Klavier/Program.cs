using MeltySynth;
using Silk.NET.OpenAL;

const int sampleRate = 44100;
const int channels = 2;

using FileStream sfFile = new("C:\\Users\\MevenCourouble\\Desktop\\GRAND PIANO.sf2", FileMode.Open, FileAccess.Read, FileShare.Read);
SoundFont soundFont = new(sfFile);
Synthesizer synthesizer = new(soundFont, sampleRate);

// Pre-render: 2s note on + 1s release
int noteOnSamples = sampleRate * 2 * channels;
int releaseSamples = sampleRate * 1 * channels;
float[] audioBuffer = new float[noteOnSamples + releaseSamples];

synthesizer.NoteOn(0, 60, 100);
synthesizer.RenderInterleaved(audioBuffer.AsSpan(0, noteOnSamples));

synthesizer.NoteOff(0, 60);
synthesizer.RenderInterleaved(audioBuffer.AsSpan(noteOnSamples, releaseSamples));

// Convert float [-1,1] to 16-bit PCM for OpenAL
short[] pcmBuffer = new short[audioBuffer.Length];
for (int i = 0; i < audioBuffer.Length; i++)
{
    float sample = Math.Clamp(audioBuffer[i], -1f, 1f);
    pcmBuffer[i] = (short)(sample * short.MaxValue);
}

// OpenAL setup
unsafe
{
    AL al = AL.GetApi(true);
    ALContext alc = ALContext.GetApi(true);

    Device* device = alc.OpenDevice(null);
    if (device == null)
    {
        Console.WriteLine("Failed to open audio device.");
        return;
    }

    Context* context = alc.CreateContext(device, null);
    alc.MakeContextCurrent(context);

    uint source = al.GenSource();
    uint buffer = al.GenBuffer();

    fixed (short* pData = pcmBuffer)
    {
        al.BufferData(buffer, BufferFormat.Stereo16, pData, pcmBuffer.Length * sizeof(short), sampleRate);
    }

    al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);

    Console.WriteLine("Playing Middle C...");
    al.SourcePlay(source);

    // Wait for playback to finish
    int state;
    do
    {
        Thread.Sleep(100);
        al.GetSourceProperty(source, GetSourceInteger.SourceState, out state);
    } while (state == (int)SourceState.Playing);

    Console.WriteLine("Playback finished.");

    al.DeleteSource(source);
    al.DeleteBuffer(buffer);
    alc.DestroyContext(context);
    alc.CloseDevice(device);

    Console.WriteLine("Audio device released.");
}
