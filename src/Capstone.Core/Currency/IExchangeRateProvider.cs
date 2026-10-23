namespace Capstone.Core.Currency;

public interface IExchangeRateProvider
{
    /// <summary>How many units of <paramref name="to"/> one unit of <paramref name="from"/> buys.</summary>
    Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default);
}