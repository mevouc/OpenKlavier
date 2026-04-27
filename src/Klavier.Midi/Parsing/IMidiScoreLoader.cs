namespace Klavier.Midi.Parsing;

public interface IMidiScoreLoader
{
    Task<MidiScore> LoadAsync(string filePath, CancellationToken ct = default);
}
