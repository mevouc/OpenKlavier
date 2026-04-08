using Klavier.Core.Ports;
using Klavier.Core.Primitives;

namespace Klavier.Core.Engine;

public interface IPianoEngine
{
    void RegisterHandler(INoteEventHandler noteEventHandler);
    void NoteOn(NotePitch keyPitch);
    void NoteOff(NotePitch keyPitch);
    void SustainOn();
    void SustainOff();
    void AllNotesOff();
}
