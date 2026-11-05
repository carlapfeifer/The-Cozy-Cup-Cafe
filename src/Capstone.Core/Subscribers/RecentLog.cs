using SharpQuest.Contracts;

namespace Capstone.Core.Subscribers;

/// <summary>Remembers the last few items added, newest first, and keeps them current when an item is updated.</summary>
public sealed class RecentLog
{
    private readonly RecentlyUsed<IItem> _recent;

    public RecentLog(int capacity) => _recent = new RecentlyUsed<IItem>(capacity);

    public IEnumerable<IItem> Items() => _recent.Items();

    public void Attach(CafeStore store)
    {
        StoreFunctions.Subscribe(store, _recent.Push);
        store.ItemUpdated += (_, change) => _recent.Replace(change.PreviousItem, change.UpdatedItem);
    }
}