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
        Resources["SystemAccentColor"] = ThemePaletteProvider.Accent;
        Resources["SystemAccentColorLight1"] = ThemePaletteProvider.AccentLight1;
        Resources["SystemAccentColorLight2"] = ThemePaletteProvider.AccentLight2;
        Resources["SystemAccentColorLight3"] = ThemePaletteProvider.AccentLight3;
        Resources["SystemAccentColorDark1"] = ThemePaletteProvider.AccentDark1;
        Resources["SystemAccentColorDark2"] = ThemePaletteProvider.AccentDark2;
        Resources["SystemAccentColorDark3"] = ThemePaletteProvider.AccentDark3;
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
