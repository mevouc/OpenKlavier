using Avalonia;

namespace Klavier.UI.Views.Controls;

/// <summary>
/// A TextButton with two-state toggle semantics: clicking flips IsToggled and fires Toggled.
/// IsActive tracks IsToggled, even when IsEnabled changes (overriding BaseButton's IsActive = IsEnabled
/// auto-sync, which is incorrect for toggles).
/// </summary>
public class ToggleTextButton : TextButton
{
    private bool _isToggled;

    public event Action<bool>? Toggled;

    public ToggleTextButton(string text) : base(text, momentaryActiveOnPress: false)
    {
        // BaseButton already flips IsActive on PointerPressed; mirror that into _isToggled and notify.
        PointerPressed += (_, e) =>
        {
            _isToggled = !_isToggled;
            Toggled?.Invoke(_isToggled);
            e.Handled = true;
        };
    }

    public bool IsToggled
    {
        get => _isToggled;
        set
        {
            if (_isToggled == value)
            {
                return;
            }
            _isToggled = value;
            IsActive = value;
            Toggled?.Invoke(_isToggled);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            // BaseButton auto-syncs IsActive to IsEnabled; toggles must keep their own state instead.
            IsActive = _isToggled;
        }
    }
}
