using Capstone.Core;

namespace Capstone.Core.Models;

public sealed class CompletedOrderHistory
{
    private readonly RecentlyUsed<Order> _recentOrders;

    public CompletedOrderHistory(int capacity = 10)
    {
        _recentOrders = new RecentlyUsed<Order>(capacity);
    }

    public int Count => _recentOrders.Count;

    // Returns a snapshot, newest order first.
    public IReadOnlyList<Order> Orders => _recentOrders.Items().ToArray();

    public void Record(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _recentOrders.Push(order);
    }
    public int Capacity => _recentOrders.Capacity;
}