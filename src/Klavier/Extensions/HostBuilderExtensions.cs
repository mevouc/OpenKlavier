using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Klavier.Extensions;

public static class HostBuilderExtensions
{
    private const string _UserSettingsFileName = "usersettings.json";
    private const string _DefaultContent = "{}";

    public static IHostBuilder UseUserSettings(this IHostBuilder builder, string appName)
    {
        return builder.ConfigureAppConfiguration((_, config) =>
        {
            string userSettingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
            string userSettingsPath = Path.Combine(userSettingsDir, _UserSettingsFileName);

            if (!File.Exists(userSettingsPath))
            {
                Directory.CreateDirectory(userSettingsDir);
                File.WriteAllText(userSettingsPath, _DefaultContent);
            }

            config.AddJsonFile(
                userSettingsPath,
                optional: true,
                reloadOnChange: true);
        });
    }
}
