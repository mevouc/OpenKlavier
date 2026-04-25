using Klavier.Extensions;
using Microsoft.Extensions.Hosting;

const string AppName = "Klavier";

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseUserSettings(AppName)
    .ConfigureAppServices()
    .Build();

host.EnsureValidUserSettings()
    .InitializePianoPipeline()
    .InitializeMidiPlaybackCoordinator()
    .AutoLoadMidi()
    .ApplyColorTheme();

return host.RunAvaloniaApp(args);
