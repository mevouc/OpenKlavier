using Klavier.Core.Primitives;

namespace Klavier.Core.Events;

public readonly record struct NoteOnEvent(
    NotePitch KeyPitch,
    NotePitch SoundingPitch,
    NoteVelocity Velocity);
