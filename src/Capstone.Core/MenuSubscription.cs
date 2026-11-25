namespace Capstone.Core;

public sealed class MenuSubscription : IDisposable
{
    private Action? _unsubscribe;

    public MenuSubscription(Action unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(unsubscribe);
        _unsubscribe = unsubscribe;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}