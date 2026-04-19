using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Klavier.UI.Theme;

namespace Klavier.UI.Views;

public abstract class ActivableControl : Border
{
    private static readonly SolidColorBrush _DefaultBackgroundBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _ActiveBackgroundBrush = new(ThemePaletteProvider.HoverHighlight);
    private static readonly SolidColorBrush _DefaultBorderBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _ActiveBorderBrush = new(ThemePaletteProvider.Accent);

    protected static readonly SolidColorBrush DefaultTextBrush = new(ThemePaletteProvider.TextPrimary);
    protected static readonly SolidColorBrush ActiveTextBrush = new(ThemePaletteProvider.AccentLight1);

    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            UpdateVisualState();
        }
    }

    protected ActivableControl()
    {
        Background = _DefaultBackgroundBrush;
        BorderBrush = _DefaultBorderBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(Constants.CornerRadius);
        Cursor = new Cursor(StandardCursorType.Hand);

        PointerEntered += (_, _) => UpdateVisualState();
        PointerExited += (_, _) => UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        Background = _isActive != IsPointerOver ? _ActiveBackgroundBrush : _DefaultBackgroundBrush;
        BorderBrush = (_isActive, IsPointerOver) switch
        {
            (true, _) => _ActiveBorderBrush,
            (false, true) => _ActiveBackgroundBrush,
            _ => _DefaultBorderBrush,
        };
        OnActiveStateChanged(_isActive);
    }

    protected abstract void OnActiveStateChanged(bool isActive);
}
