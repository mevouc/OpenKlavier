namespace Klavier.Midi.Ports;

public interface IMidiScoreLoader
{
    Task<MidiScore> LoadAsync(string filePath, CancellationToken ct = default);
}
