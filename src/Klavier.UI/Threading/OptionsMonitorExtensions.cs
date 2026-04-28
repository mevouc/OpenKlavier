using Avalonia.Threading;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Threading;

public static class OptionsMonitorExtensions
{
    public static IDisposable? OnChangeOnUIThread<T>(this IOptionsMonitor<T> monitor, Action<T> handler)
        => monitor.OnChange(value => Dispatcher.UIThread.Post(() => handler(value)));
}
