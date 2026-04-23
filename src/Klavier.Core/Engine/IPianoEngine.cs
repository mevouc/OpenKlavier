using Klavier.Core.Ports;
using Klavier.Core.Primitives;

namespace Klavier.Core.Engine;

public interface IPianoEngine
{
    event Action? PanicRaised;

    void RegisterHandler(INoteEventHandler noteEventHandler);
    void NoteOn(NotePitch keyPitch, NoteVelocity? velocity = null);
    void NoteOff(NotePitch keyPitch);
    void SustainOn(InputSource source = InputSource.User);
    void SustainOff(InputSource source = InputSource.User);
    void ToggleSustain(InputSource source = InputSource.User);
    void Panic();
}
