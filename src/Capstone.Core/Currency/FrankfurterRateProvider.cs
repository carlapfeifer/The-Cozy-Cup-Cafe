using System.Text.Json;
using System.Collections.Concurrent;

namespace Capstone.Core.Currency;

public sealed class FrankfurterRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<(string From, string To), decimal> _rateCache = new();

    public FrankfurterRateProvider(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    public async Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);

        from = from.ToUpperInvariant();
        to = to.ToUpperInvariant();
        if (from == to) return 1m; // no request needed
        if (_rateCache.TryGetValue((from, to), out var cachedRate))
            return cachedRate;

        var url =
            $"https://api.frankfurter.dev/v1/latest?base={Uri.EscapeDataString(from)}&symbols={Uri.EscapeDataString(to)}";

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode(); // throws HttpRequestException on 4xx/5xx

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (doc.RootElement.TryGetProperty("rates", out var rates)
            && rates.TryGetProperty(to, out var rate)
            && rate.TryGetDecimal(out var value))
        {
            _rateCache.TryAdd((from, to), value);
            return value;
        }

        throw new InvalidOperationException($"The response has no {from} to {to} rate.");
    }
}