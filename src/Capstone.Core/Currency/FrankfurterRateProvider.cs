using System.Text.Json;

namespace Capstone.Core.Currency;

public sealed class FrankfurterRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _http;

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
        if (from == to) return 1m;   // no request needed

        var url = $"https://api.frankfurter.dev/v1/latest?base={Uri.EscapeDataString(from)}&symbols={Uri.EscapeDataString(to)}";

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();   // throws HttpRequestException on 4xx/5xx

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (doc.RootElement.TryGetProperty("rates", out var rates)
            && rates.TryGetProperty(to, out var rate)
            && rate.TryGetDecimal(out var value))
            return value;

        throw new InvalidOperationException($"The response has no {from} to {to} rate.");
    }
}