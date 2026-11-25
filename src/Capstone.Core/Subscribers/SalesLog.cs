using Capstone.Core;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Subscribers;


public sealed class SalesLog
{
    private readonly List<OrderCompletedChange> _completedOrders = new();
    private readonly List<IItem> _soldOutItems = new();

    public IReadOnlyList<OrderCompletedChange> CompletedOrders =>
        _completedOrders.ToArray();

    public IReadOnlyList<IItem> SoldOutItems =>
        _soldOutItems.ToArray();

    public MenuSubscription Attach(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        EventHandler<OrderCompletedChange> orderHandler =
            (_, change) => _completedOrders.Add(change);

        EventHandler<ItemSoldOutChange> soldOutHandler =
            (_, change) => _soldOutItems.Add(change.Item);

        store.OrderCompleted += orderHandler;
        store.ItemSoldOut += soldOutHandler;

        return new MenuSubscription(() =>
        {
            store.OrderCompleted -= orderHandler;
            store.ItemSoldOut -= soldOutHandler;
        });
    }
}