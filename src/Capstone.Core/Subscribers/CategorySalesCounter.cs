namespace Capstone.Core.Subscribers;

public sealed class CategorySalesCounter
{
    private readonly Dictionary<string, int> _unitsSold =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> UnitsSold =>
        new Dictionary<string, int>(_unitsSold, StringComparer.OrdinalIgnoreCase);

    public MenuSubscription Attach(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        EventHandler<OrderCompletedChange> handler = (_, change) =>
        {
            foreach (var line in change.Order.Lines)
            {
                var categoryId = line.Item.Category.Id;
                _unitsSold[categoryId] =
                    _unitsSold.GetValueOrDefault(categoryId) + line.Quantity;
            }
        };

        store.OrderCompleted += handler;
        return new MenuSubscription(() => store.OrderCompleted -= handler);
    }
}