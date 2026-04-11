using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Klavier.UI.Theme;
using Klavier.UI.Views;

namespace Klavier.UI;

public class App(
    Func<MainWindow> mainWindowFactory)
    : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Resources["SystemAccentColor"] = KlavierTheme.Accent;
        Resources["SystemAccentColorLight1"] = KlavierTheme.AccentLight1;
        Resources["SystemAccentColorLight2"] = KlavierTheme.AccentLight2;
        Resources["SystemAccentColorLight3"] = KlavierTheme.AccentLight3;
        Resources["SystemAccentColorDark1"] = KlavierTheme.AccentDark1;
        Resources["SystemAccentColorDark2"] = KlavierTheme.AccentDark2;
        Resources["SystemAccentColorDark3"] = KlavierTheme.AccentDark3;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindowFactory();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
