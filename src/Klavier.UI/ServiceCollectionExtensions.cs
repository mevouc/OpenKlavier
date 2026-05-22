using Klavier.Core.Engine;
using Klavier.UI.Input;
using Klavier.UI.Input.Mapping;
using Klavier.Config.Schema;
using Klavier.Config.UserSettings;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Player;
using Klavier.UI.Views.Settings;
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

        services.AddSingleton<IKeyboardMappingService, KeyboardMappingService>();
        services.AddSingleton<PianoViewModel>();
        services.AddSingleton<IPianoKeyState>(sp => sp.GetRequiredService<PianoViewModel>());
        services.AddSingleton<PlayerViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<KeyboardInputHandler>();
        services.AddTransient<SustainBarControl>();
        services.AddTransient<DropOverlay>();
        services.AddTransient<PianoView>(sp => new PianoView(
            sp.GetRequiredService<PianoViewModel>().Keys,
            sp.GetRequiredService<SustainBarControl>()));
        services.AddTransient<FallingNotesView>();
        services.AddTransient<PlayerBarView>();
        services.AddTransient<ProgressBarView>();
        services.AddTransient<PlayerView>();
        services.AddTransient<ToolbarView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<MainWindow>();

        services.AddSingleton<Func<KeyboardMapping, string?, KeybindsEditorWindow>>(sp =>
            (clone, existingLayoutName) => new KeybindsEditorWindow(
                clone,
                existingLayoutName,
                sp.GetRequiredService<IPianoEngine>(),
                sp.GetRequiredService<IOptionsMonitor<UIConfig>>(),
                sp.GetRequiredService<IOptionsMonitor<PianoConfig>>(),
                sp.GetRequiredService<IUserSettingsService>(),
                sp.GetRequiredService<IKeyboardMappingService>()));

        return services;
    }
}
