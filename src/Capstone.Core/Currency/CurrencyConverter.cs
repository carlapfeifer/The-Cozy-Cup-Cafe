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

    public async Task<Money> ConvertAsync(Money amount, string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);

        var normalizedTarget = targetCurrency.ToUpperInvariant();

        if (!Money.IsSupportedCurrency(normalizedTarget))
        {
            throw new ArgumentException(
                $"Unsupported currency '{targetCurrency}'.",
                nameof(targetCurrency));
        }

        if (string.Equals(
                amount.Currency,
                normalizedTarget,
                StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var rate = await _rates.GetRateAsync(amount.Currency, targetCurrency, cancellationToken);
        if (rate <= 0)
            throw new InvalidOperationException("The exchange rate must be positive.");

        return new Money(
            Math.Round(amount.Amount * rate, 2, MidpointRounding.AwayFromZero),
            targetCurrency.ToUpperInvariant());
    }

    public async Task<IReadOnlyList<(MenuItem Item, Money Price)>> ConvertMenuPricesAsync(
        IEnumerable<MenuItem> items,
        string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);

        var normalizedTarget = targetCurrency.ToUpperInvariant();
        if (!Money.IsSupportedCurrency(normalizedTarget))
        {
            throw new ArgumentException(
                $"Unsupported currency '{targetCurrency}'.",
                nameof(targetCurrency));
        }

        var menuItems = items.ToArray();
        if (menuItems.Any(item => item is null))
            throw new ArgumentException("The menu cannot contain a null item.", nameof(items));

        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceCurrency in menuItems
                     .Select(item => item.Price.Currency)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var rate = string.Equals(
                sourceCurrency,
                normalizedTarget,
                StringComparison.OrdinalIgnoreCase)
                ? 1m
                : await _rates.GetRateAsync(
                    sourceCurrency,
                    normalizedTarget,
                    cancellationToken);

            if (rate <= 0)
                throw new InvalidOperationException("The exchange rate must be positive.");

            rates.Add(sourceCurrency, rate);
        }

        return menuItems
            .Select(item => (
                Item: item,
                Price: new Money(
                    Math.Round(
                        item.Price.Amount * rates[item.Price.Currency],
                        2,
                        MidpointRounding.AwayFromZero),
                    normalizedTarget)))
            .ToArray();
    }

    public Task<Money> ConvertTotalAsync(Order order, string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        return ConvertAsync(new Money(order.Total, order.Currency), targetCurrency, cancellationToken);
    }
}