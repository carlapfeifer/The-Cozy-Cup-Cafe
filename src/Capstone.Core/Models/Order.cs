namespace Capstone.Core.Models;

public class Order
{
    private readonly List<OrderLine> _lines = new();

    public string Currency { get; }

    /// <summary>When the order was placed. Decides time-based prices (food after 18:00).</summary>
    public DateTime PlacedAt { get; }

    /// <summary>Discount in percent, from 0 to 100.</summary>
    public decimal DiscountPercent { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.ToArray();

    public Order(string currency = "RON", DateTime? placedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Currency = currency;
        PlacedAt = placedAt ?? DateTime.Now;
    }

    public void Add(MenuItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Price.Currency != Currency)
            throw new ArgumentException(
                $"Item is priced in {item.Price.Currency}, but the order is in {Currency}.", nameof(item));
        if (!item.IsAvailable)
            throw new InvalidOperationException($"'{item.DisplayName}' is not available.");

        _lines.Add(new OrderLine(item, quantity));   // rejects a quantity below 1
    }

    public void ApplyDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Discount must be between 0 and 100.");

        DiscountPercent = percent;
    }

    /// <summary>The price of one unit of this line at the time of the order (size and time rules applied).</summary>
    public decimal UnitPriceOf(OrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return Pricing.FinalPrice(line.Item, TimeOnly.FromDateTime(PlacedAt));
    }

    /// <summary>Sum of all lines at their order-time prices, before the discount. 0 for an empty order.</summary>
    public decimal Subtotal => _lines.Sum(line => UnitPriceOf(line) * line.Quantity);

    /// <summary>Amount to pay: discount applied, rounded to the cent (half rounds up).</summary>
    public decimal Total =>
        Math.Round(Subtotal * (1 - DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
}