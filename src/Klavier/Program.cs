using Klavier.Extensions;
using Microsoft.Extensions.Hosting;

namespace Klavier;

public static class Program
{
    private const string _AppName = "Klavier";

    // [STAThread] is required for Avalonia drag-and-drop on Windows (OLE/COM uses STA).
    [STAThread]
    public static int Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .UseUserSettings(_AppName)
            .ConfigureAppServices()
            .Build();

        host.EnsureValidUserSettings()
            .InitializePianoPipeline()
            .InitializeMidiPlaybackCoordinator()
            .AutoLoadMidi()
            .ApplyColorTheme();

        return host.RunAvaloniaApp(args);
    }
}
