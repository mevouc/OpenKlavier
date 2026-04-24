using Klavier.Core.Engine;
using Klavier.UI.Input;
using Klavier.UI.Input.Mapping;
using Klavier.Config;
using Klavier.UI.Ports;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Player;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Klavier.UI.Views.Settings.KeybindsEditor;

namespace Klavier.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UIConfig>(configuration.GetSection(UIConfig.SectionName));

        services.AddSingleton<PianoViewModel>();
        services.AddSingleton<KeyboardInputHandler>();
        services.AddTransient<SustainBarControl>();
        services.AddTransient<PianoView>(sp => new PianoView(
            sp.GetRequiredService<PianoViewModel>().Keys,
            sp.GetRequiredService<SustainBarControl>()));
        services.AddTransient<FallingNotesView>();
        services.AddTransient<PlayerView>();
        services.AddTransient<ToolbarView>();
        services.AddTransient<SettingsPanel>();
        services.AddTransient<MainWindow>();

        services.AddSingleton<Func<KeyboardMapping, string?, KeybindsEditorWindow>>(sp =>
            (clone, existingLayoutName) => new KeybindsEditorWindow(
                clone,
                existingLayoutName,
                sp.GetRequiredService<IPianoEngine>(),
                sp.GetRequiredService<IOptionsMonitor<UIConfig>>(),
                sp.GetRequiredService<IOptionsMonitor<PianoConfig>>(),
                sp.GetRequiredService<IUserSettingsService>()));

        return services;
    }
}
