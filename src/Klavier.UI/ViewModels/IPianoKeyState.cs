using Klavier.Core.Primitives;

namespace Klavier.UI.ViewModels;

public interface IPianoKeyState
{
    bool IsPitchPressed(NotePitch pitch);
}
