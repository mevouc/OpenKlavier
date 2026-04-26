using Avalonia;

namespace Klavier.UI.Views.Controls;

public abstract class CustomButtonBase : ActivableControl
{
    private const double _DefaultHeight = 32;

    protected CustomButtonBase(bool momentaryActiveOnPress)
    {
        Height = _DefaultHeight;
        if (momentaryActiveOnPress)
        {
            PointerPressed += (_, _) => IsActive = true;
            PointerReleased += (_, _) => IsActive = false;
        }
        else
        {
            PointerPressed += (_, _) => IsActive = !IsActive;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            Opacity = IsEnabled ? 1.0 : 0.5;
            IsActive = IsEnabled;
        }
    }
}
