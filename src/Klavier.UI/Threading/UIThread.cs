using Avalonia.Threading;

namespace Klavier.UI.Threading;

public static class UIThread
{
    public static Action Post(Action handler)
        => () => Dispatcher.UIThread.Post(handler);

    public static Action<T> Post<T>(Action<T> handler)
        => arg => Dispatcher.UIThread.Post(() => handler(arg));
}
