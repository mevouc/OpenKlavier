using Klavier.Core.Ports;
using Klavier.Core.Primitives;

namespace Klavier.Core.Engine;

public interface IPianoEngine
{
    event Action? PanicRaised;

    void RegisterHandler(INoteEventHandler noteEventHandler);
    void NoteOn(NotePitch keyPitch, NoteVelocity? velocity = null, InputSource source = InputSource.User);
    void NoteOff(NotePitch keyPitch, InputSource source = InputSource.User);
    void AllNotesOff(InputSource source);
    void SustainOn(InputSource source = InputSource.User);
    void SustainOff(InputSource source = InputSource.User);
    void ToggleSustain(InputSource source = InputSource.User);
    void Panic();
}
