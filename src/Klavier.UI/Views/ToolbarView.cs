using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Core.Engine;
using Klavier.UI.Theme;

namespace Klavier.UI.Views;

public class ToolbarView : Border
{
    public ToolbarView(IPianoEngine pianoEngine)
    {
        Background = new SolidColorBrush(KlavierTheme.PanelBackground);
        Padding = new Thickness(8, 4);

        Button panicButton = new()
        {
            Content = "Panic",
            VerticalAlignment = VerticalAlignment.Center,
        };

        panicButton.Click += (_, _) => pianoEngine.AllNotesOff();

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { panicButton },
        };
    }
}
