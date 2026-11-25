namespace Capstone.Core.Subscribers;

public sealed class TodayTakings
{
    private readonly Dictionary<string, decimal> _totalsByCurrency =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> TotalsByCurrency =>
        new Dictionary<string, decimal>(_totalsByCurrency);

    public MenuSubscription Attach(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        EventHandler<OrderCompletedChange> handler = (_, change) =>
        {
            if (change.CompletedAt.Date != DateTime.Today)
                return;

            var currency = change.Total.Currency;
            _totalsByCurrency[currency] =
                _totalsByCurrency.GetValueOrDefault(currency) + change.Total.Amount;
        };

        store.OrderCompleted += handler;
        return new MenuSubscription(() => store.OrderCompleted -= handler);
    }
}