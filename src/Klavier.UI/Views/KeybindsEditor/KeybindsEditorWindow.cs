using Avalonia.Controls;
using Avalonia.Media;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.KeybindsEditor;

public class KeybindsEditorWindow : Window
{
    private const string _WindowTitle = "Keybinds Editor";
    private const int _DefaultWidth = 800;
    private const int _DefaultHeight = 500;

    private readonly KeyboardMapping _cloneSource;
    private readonly string? _existingLayoutName;

    public KeybindsEditorWindow(KeyboardMapping cloneSource, string? existingLayoutName)
    {
        _cloneSource = cloneSource;
        _existingLayoutName = existingLayoutName;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);
    }
}
