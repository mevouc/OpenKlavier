using Klavier.Core.Primitives;

namespace Klavier.Midi;

public readonly record struct MidiNote(
    NotePitch Pitch,
    TimeSpan Start,
    TimeSpan Duration,
    NoteVelocity Velocity);
