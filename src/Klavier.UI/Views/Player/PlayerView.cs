using Avalonia.Controls;
using Avalonia.Threading;
using Klavier.Midi.Player;

namespace Klavier.UI.Views.Player;

public class PlayerView : DockPanel
{
    public PlayerView(IMidiPlayer player, FallingNotesView fallingNotes)
    {
        Children.Add(fallingNotes);
        IsVisible = player.CurrentScore is not null;
        player.Loaded += _ => Dispatcher.UIThread.Post(() => IsVisible = true);
    }
}
