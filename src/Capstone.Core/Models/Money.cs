namespace Capstone.Core.Models;

public record Money(decimal Amount, string Currency)
{
    public decimal Amount { get; init; } =
        Amount >= 0
            ? Amount
            : throw new ArgumentOutOfRangeException(
                nameof(Amount),
                "Amount cannot be negative.");

    public string Currency { get; init; } =
        IsSupportedCurrency(Currency)
            ? Currency.ToUpperInvariant()
            : throw new ArgumentException(
                $"Unsupported currency '{Currency}'.",
                nameof(Currency));

    public static bool IsSupportedCurrency(string currency) =>
        !string.IsNullOrWhiteSpace(currency)
        && (currency.Equals("RON", StringComparison.OrdinalIgnoreCase)
            || currency.Equals("EUR", StringComparison.OrdinalIgnoreCase)
            || currency.Equals("USD", StringComparison.OrdinalIgnoreCase));
}