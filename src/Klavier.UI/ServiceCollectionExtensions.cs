using Klavier.UI.Input;
using Klavier.UI.Options;
using Klavier.UI.ViewModels;
using Klavier.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddTransient<PianoView>();
        services.AddTransient<ToolbarView>();
        services.AddTransient<MainWindow>();

        return services;
    }
}
