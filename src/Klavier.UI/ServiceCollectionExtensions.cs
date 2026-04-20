using Klavier.Core.Engine;
using Klavier.UI.Input;
using Klavier.UI.Input.Mapping;
using Klavier.Config;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Klavier.UI.Views.KeybindsEditor;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Toolbar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Klavier.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKlavierUI(
        this IServiceCollection services,
        IConfigurationSection uiSection)
    {
        services.Configure<UIConfig>(uiSection);

        services.AddSingleton<PianoViewModel>();
        services.AddSingleton<KeyboardInputHandler>();
        services.AddTransient<SustainBarControl>();
        services.AddTransient<PianoView>(sp => new PianoView(
            sp.GetRequiredService<PianoViewModel>().Keys,
            sp.GetRequiredService<SustainBarControl>()));
        services.AddTransient<ToolbarView>();
        services.AddTransient<SettingsPanel>();
        services.AddTransient<MainWindow>();

        services.AddSingleton<Func<KeyboardMapping, string?, KeybindsEditorWindow>>(sp =>
            (clone, existingLayoutName) => new KeybindsEditorWindow(
                clone,
                existingLayoutName,
                sp.GetRequiredService<IPianoEngine>(),
                sp.GetRequiredService<IOptionsMonitor<UIConfig>>(),
                sp.GetRequiredService<IOptionsMonitor<PianoConfig>>()));

        return services;
    }
}
