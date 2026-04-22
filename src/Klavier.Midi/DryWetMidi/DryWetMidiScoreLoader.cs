using Klavier.Core.Primitives;
using Klavier.Midi.Ports;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Klavier.Midi.DryWetMidi;

public class DryWetMidiScoreLoader : IMidiScoreLoader
{
    private const int _SustainController = 64; // MIDI CC64
    private const int _SustainOnThreshold = 64;

    public async Task<MidiScore> LoadAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            MidiFile file = MidiFile.Read(filePath);
            TempoMap tempoMap = file.GetTempoMap();

            List<MidiNote> notes = ParseNotes(file, tempoMap);
            List<MidiSustainEvent> sustainEvents = ParseSustainEvents(file, tempoMap);
            TimeSpan totalDuration = (TimeSpan)file.GetDuration<MetricTimeSpan>();
            string displayName = ExtractTrackName(file) ?? Path.GetFileNameWithoutExtension(filePath);

            return new MidiScore(
                FilePath: filePath,
                DisplayName: displayName,
                TotalDuration: totalDuration,
                Notes: notes,
                SustainEvents: sustainEvents);
        }
        catch (MidiException ex)
        {
            throw new InvalidDataException($"Failed to parse MIDI file '{filePath}'.", ex);
        }
    }

    private static List<MidiNote> ParseNotes(MidiFile file, TempoMap tempoMap)
    {
        List<MidiNote> notes = [];
        foreach (Note note in file.GetNotes())
        {
            MetricTimeSpan start = note.TimeAs<MetricTimeSpan>(tempoMap);
            MetricTimeSpan length = note.LengthAs<MetricTimeSpan>(tempoMap);
            notes.Add(new MidiNote(
                new NotePitch((byte)note.NoteNumber),
                (TimeSpan)start,
                (TimeSpan)length,
                new NoteVelocity((byte)note.Velocity)));
        }
        return notes;
    }

    private static List<MidiSustainEvent> ParseSustainEvents(MidiFile file, TempoMap tempoMap)
    {
        List<MidiSustainEvent> events = [];
        foreach (TimedEvent timedEvent in file.GetTimedEvents())
        {
            if (timedEvent.Event is not ControlChangeEvent cc || (byte)cc.ControlNumber != _SustainController)
            {
                continue;
            }
            MetricTimeSpan at = TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap);
            bool isOn = (byte)cc.ControlValue >= _SustainOnThreshold;
            events.Add(new MidiSustainEvent((TimeSpan)at, isOn));
        }
        return events;
    }

    private static string? ExtractTrackName(MidiFile file)
    {
        foreach (TrackChunk track in file.GetTrackChunks())
        {
            foreach (MidiEvent ev in track.Events)
            {
                if (ev is SequenceTrackNameEvent nameEvent)
                {
                    return nameEvent.Text;
                }
            }
        }
        return null;
    }
}
