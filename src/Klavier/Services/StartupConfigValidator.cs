using Klavier.Config;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.Services;

/// <summary>
/// Runs a series of semantic checks on loaded configuration at startup. Any setting that points to
/// something missing or otherwise invalid logs a warning and clears the user-side override via
/// <see cref="IUserSettingsService.ClearSetting"/>, letting the layered config fall back to the
/// shipped default in <c>appsettings.json</c>.
/// </summary>
public static class StartupConfigValidator
{
    public static void ValidateAndHeal(IServiceProvider services, ILogger logger)
    {
        ValidateKeyboardLayout(services, logger);
        ValidateSoundFontPath(services, logger);
    }

    private static void ValidateKeyboardLayout(IServiceProvider services, ILogger logger)
    {
        IOptions<UIConfig> uiConfig = services.GetRequiredService<IOptions<UIConfig>>();
        IUserSettingsService settings = services.GetRequiredService<IUserSettingsService>();

        string requested = uiConfig.Value.KeyboardLayout;
        string[] available = KeyboardMappingProvider.GetAvailableLayouts();

        if (available.Contains(requested, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogWarning(
            "Keyboard layout '{Requested}' not found. Clearing user override, falling back to appsettings.json default.",
            requested);

        settings.ClearSetting(ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.KeyboardLayout)));
    }

    private static void ValidateSoundFontPath(IServiceProvider services, ILogger logger)
    {
        IOptions<AudioConfig> audioConfig = services.GetRequiredService<IOptions<AudioConfig>>();
        IUserSettingsService settings = services.GetRequiredService<IUserSettingsService>();

        string requested = audioConfig.Value.SoundFont.Path;
        if (File.Exists(requested))
        {
            return;
        }

        logger.LogWarning(
            "SoundFont file '{Requested}' not found. Clearing user override, falling back to appsettings.json default.",
            requested);

        settings.ClearSetting(ConfigKey.Of(
            AudioConfig.SectionName,
            nameof(AudioConfig.SoundFont),
            nameof(Config.SoundFontConfig.Path)));
    }
}
