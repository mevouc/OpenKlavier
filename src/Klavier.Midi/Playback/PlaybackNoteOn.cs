using Klavier.Core.Primitives;

namespace Klavier.Midi.Playback;

public readonly record struct PlaybackNoteOn(NotePitch Pitch, NoteVelocity Velocity);
