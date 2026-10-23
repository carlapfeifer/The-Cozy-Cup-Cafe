using Capstone.Core.Models;

namespace Capstone.Core.Currency;

public sealed class CurrencyConverter
{
    private readonly IExchangeRateProvider _rates;

    public CurrencyConverter(IExchangeRateProvider rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        _rates = rates;
    }

    public async Task<Money> ConvertAsync(Money amount, string targetCurrency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);

        if (string.Equals(amount.Currency, targetCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var rate = await _rates.GetRateAsync(amount.Currency, targetCurrency, cancellationToken);
        if (rate <= 0)
            throw new InvalidOperationException("The exchange rate must be positive.");

        return new Money(
            Math.Round(amount.Amount * rate, 2, MidpointRounding.AwayFromZero),
            targetCurrency.ToUpperInvariant());
    }
    
    public Task<Money> ConvertTotalAsync(Order order, string targetCurrency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        return ConvertAsync(new Money(order.Total, order.Currency), targetCurrency, cancellationToken);
    }
}