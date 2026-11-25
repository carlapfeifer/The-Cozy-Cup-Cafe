using Capstone.Core.Models;

namespace Capstone.Core.Subscribers;

public sealed class FeaturedMenuCache
{
    private MenuItem[]? _cachedItems;

    public IReadOnlyList<MenuItem> GetFeaturedItems(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _cachedItems ??= store
            .OfType<MenuItem>()
            .Where(item => item.IsFeatured && item.IsAvailable)
            .ToArray();

        return _cachedItems.ToArray();
    }

    public MenuSubscription Attach(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        EventHandler<MenuChangedChange> handler = (_, _) => _cachedItems = null;

        store.MenuChanged += handler;
        return new MenuSubscription(() => store.MenuChanged -= handler);
    }
}