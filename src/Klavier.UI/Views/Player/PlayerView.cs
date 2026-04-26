using Avalonia.Controls;

namespace Klavier.UI.Views.Player;

public class PlayerView : DockPanel
{
    public PlayerView(
        PlayerBarView playerBar,
        ProgressBarView progressBar,
        FallingNotesView fallingNotes)
    {
        DockPanel.SetDock(playerBar, Dock.Top);
        Children.Add(playerBar);

        DockPanel.SetDock(progressBar, Dock.Top);
        Children.Add(progressBar);

        Children.Add(fallingNotes);
    }
}
