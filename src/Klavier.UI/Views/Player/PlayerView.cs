using Avalonia.Controls;
using Avalonia.Threading;
using Klavier.Midi.Player;

namespace Klavier.UI.Views.Player;

public class PlayerView : DockPanel
{
    public PlayerView(IMidiPlayer player, PlayerBarView playerBar, FallingNotesView fallingNotes)
    {
        DockPanel.SetDock(playerBar, Dock.Top);
        Children.Add(playerBar);
        Children.Add(fallingNotes);
        IsVisible = player.CurrentScore is not null;
        player.Loaded += _ => Dispatcher.UIThread.Post(() => IsVisible = true);
    }
}
