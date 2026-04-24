using Avalonia;

namespace Klavier.UI.Views.Controls;

public abstract class CustomButtonBase : ActivableControl
{
    protected CustomButtonBase(bool momentaryActiveOnPress)
    {
        if (momentaryActiveOnPress)
        {
            PointerPressed += (_, _) => IsActive = true;
            PointerReleased += (_, _) => IsActive = false;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            Opacity = IsEnabled ? 1.0 : 0.5;
        }
    }
}
