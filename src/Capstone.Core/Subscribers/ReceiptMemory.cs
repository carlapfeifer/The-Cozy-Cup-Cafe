namespace Capstone.Core.Subscribers;

public sealed class ReceiptMemory
{
    private readonly List<string> _receipts = new();

    public IReadOnlyList<string> Receipts => _receipts.ToArray();

    public MenuSubscription Attach(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        EventHandler<OrderCompletedChange> handler = (_, change) =>
        {
            var lines = string.Join(
                ", ",
                change.Order.Lines.Select(line =>
                    $"{line.Quantity}x {line.Item.DisplayName}"));

            var receipt =
                $"{change.CompletedAt:yyyy-MM-dd HH:mm} | {lines} | " +
                $"Paid {change.Total.Amount:0.00} {change.Total.Currency}";

            _receipts.Add(receipt);
        };

        store.OrderCompleted += handler;
        return new MenuSubscription(() => store.OrderCompleted -= handler);
    }
}