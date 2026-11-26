namespace Capstone.Core.Models;

using Capstone.Core.Currency;


public class Order
{
    private readonly List<OrderLine> _lines = new();

    public string Currency { get; }

    /// <summary>When the order was placed. Decides time-based prices (food after 18:00).</summary>
    public DateTime PlacedAt { get; }

    /// <summary>Discount in percent, from 0 to 100.</summary>
    public decimal DiscountPercent { get; private set; }
    public ServiceMode ServiceMode { get; private set; }
    
    public PaymentMethod PaymentMethod { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines.ToArray();

    public Order(
        string currency = "RON", 
        DateTime? placedAt = null,
        ServiceMode servicemode = ServiceMode.DineIn,
        PaymentMethod paymentMethod = PaymentMethod.Cash
        )
        
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Currency = currency;
        PlacedAt = placedAt ?? DateTime.Now;
        ServiceMode = servicemode;
        if (!Enum.IsDefined(paymentMethod))
            throw new ArgumentOutOfRangeException(nameof(paymentMethod));

        PaymentMethod = paymentMethod;
    }

    public void Add(
        MenuItem item,
        int quantity,
        MilkOption milk = MilkOption.Regular,
        SyrupOption syrup = SyrupOption.None)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Price.Currency);

        if (!item.IsAvailableAt(TimeOnly.FromDateTime(PlacedAt)))
            throw new InvalidOperationException($"'{item.DisplayName}' is not available.");

        if (!string.Equals(item.Price.Currency, Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Item is priced in {item.Price.Currency}, but the order is in {Currency}.",
                nameof(item));
        }

        var existingIndex = _lines.FindIndex(line =>
            line.Item.Id == item.Id && line.Milk == milk && line.Syrup == syrup);
        if (existingIndex >= 0)
        {
            var existingLine = _lines[existingIndex];
            _lines[existingIndex] = new OrderLine(
                existingLine.Item,
                checked(existingLine.Quantity + quantity),
                milk,
                syrup);
            return;
        }

        _lines.Add(new OrderLine(item, quantity, milk, syrup));
    }
    
    public bool RemoveLine(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        var index = _lines.FindIndex(line => line.Item.Id == itemId);
        if (index < 0)
            return false;

        _lines.RemoveAt(index);
        return true;
    }
    public bool RemoveOneFromLine(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        var index = _lines.FindIndex(line => line.Item.Id == itemId);
        if (index < 0)
            return false;

        var line = _lines[index];

        if (line.Quantity == 1)
            _lines.RemoveAt(index);
        else
            _lines[index] = new OrderLine(line.Item, line.Quantity - 1);

        return true;
    }
    
    public async Task<Money> ConvertTotalAsync(
        CurrencyConverter converter,
        string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(converter);

        return await converter.ConvertTotalAsync(
            this,
            targetCurrency,
            cancellationToken);
    }
    public void ApplyDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Discount must be between 0 and 100.");

        DiscountPercent = percent;
    }

    public void SetServiceDetails(ServiceMode serviceMode, PaymentMethod paymentMethod)
    {
        if (!Enum.IsDefined(serviceMode))
            throw new ArgumentOutOfRangeException(nameof(serviceMode));
        if (!Enum.IsDefined(paymentMethod))
            throw new ArgumentOutOfRangeException(nameof(paymentMethod));

        ServiceMode = serviceMode;
        PaymentMethod = paymentMethod;
    }

    /// <summary>The price of one unit of this line at the time of the order (size and time rules applied).</summary>
    public decimal UnitPriceOf(OrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return line.Item.FinalPrice(TimeOnly.FromDateTime(PlacedAt))
               + line.CustomizationSurcharge;
    }

    /// <summary>Sum of all lines at their order-time prices, before the discount. 0 for an empty order.</summary>
    public decimal Subtotal => _lines.Sum(line => UnitPriceOf(line) * line.Quantity);

    /// <summary>Amount to pay: discount applied, rounded to the cent (half rounds up).</summary>
    public decimal Total =>
        Math.Round(Subtotal * (1 - DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
    
   public IReadOnlyDictionary<decimal, decimal> VatBreakdown
{
    get
    {
        var grossByPercent = new Dictionary<decimal, decimal>();
        var discountMultiplier = 1m - DiscountPercent / 100m;

        foreach (var line in _lines)
        {
            if (line.Item is Combo combo)
            {
                var partsSubtotal = combo.Parts.Sum(part =>
                    Pricing.FinalPrice(
                        part,
                        TimeOnly.FromDateTime(PlacedAt)));

                if (partsSubtotal == 0m)
                    continue;

                var comboGross =
                    UnitPriceOf(line)
                    * line.Quantity
                    * discountMultiplier;

                foreach (var part in combo.Parts)
                {
                    var partGross =
                        Pricing.FinalPrice(
                            part,
                            TimeOnly.FromDateTime(PlacedAt));

                    var allocatedGross =
                        comboGross * partGross / partsSubtotal;

                    AddGross(
                        grossByPercent,
                        part.Vat.PercentFor(ServiceMode),
                        allocatedGross);
                }
            }
            else
            {
                var gross =
                    UnitPriceOf(line)
                    * line.Quantity
                    * discountMultiplier;
                
                AddGross(
                    grossByPercent,
                    line.Item.Vat.PercentFor(ServiceMode),
                    gross);
            }
        }

        return grossByPercent.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                var divisor = 1m + pair.Key / 100m;

                return Math.Round(
                    pair.Value - pair.Value / divisor,
                    2,
                    MidpointRounding.AwayFromZero);
            });
    }
}

public decimal VatTotal =>
    Math.Round(
        VatBreakdown.Values.Sum(),
        2,
        MidpointRounding.AwayFromZero);

private static void AddGross(
    IDictionary<decimal, decimal> totals,
    decimal percent,
    decimal gross)
{
    if (totals.ContainsKey(percent))
        totals[percent] += gross;
    else
        totals[percent] = gross;
}

}