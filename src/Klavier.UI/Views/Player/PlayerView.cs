using Avalonia.Controls;
using Avalonia.Threading;
using Klavier.Midi.Player;

namespace Klavier.UI.Views.Player;

public class PlayerView : DockPanel
{
    private const double _DefaultHeight = 200;

    public PlayerView(IMidiPlayer player, FallingNotesView fallingNotes)
    {
        Height = _DefaultHeight;
        Children.Add(fallingNotes);
        IsVisible = player.CurrentScore is not null;
        player.Loaded += _ => Dispatcher.UIThread.Post(() => IsVisible = true);
    }
}
