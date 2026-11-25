using System.Net;
using Capstone.Core.Currency;

namespace Capstone.Tests;

public class FrankfurterRateProviderTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? LastRequest { get; private set; }
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request.RequestUri;
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static FrankfurterRateProvider ProviderFor(StubHandler handler) =>
        new(new HttpClient(handler));

    private const string RonToEur = """{"base":"RON","date":"2026-10-23","rates":{"EUR":0.2}}""";

    [Fact]
    public async Task Rate_is_read_from_the_response()
    {
        var provider = ProviderFor(new StubHandler(HttpStatusCode.OK, RonToEur));

        var rate = await provider.GetRateAsync("RON", "EUR");

        Assert.Equal(0.2m, rate);
    }

    [Fact]
    public async Task Request_names_both_currencies()
    {
        var handler = new StubHandler(HttpStatusCode.OK, RonToEur);

        await ProviderFor(handler).GetRateAsync("ron", "eur");

        Assert.Contains("base=RON", handler.LastRequest!.Query);
        Assert.Contains("symbols=EUR", handler.LastRequest!.Query);
    }

    [Fact]
    public async Task Server_error_is_reported()
    {
        var provider = ProviderFor(new StubHandler(HttpStatusCode.InternalServerError, ""));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetRateAsync("RON", "EUR"));
    }

    [Fact]
    public async Task Response_without_the_rate_is_reported()
    {
        var provider = ProviderFor(new StubHandler(HttpStatusCode.OK, """{"base":"RON","rates":{}}"""));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRateAsync("RON", "EUR"));
    }

    [Fact]
    public async Task Same_currency_needs_no_request()
    {
        var handler = new StubHandler(HttpStatusCode.OK, RonToEur);

        var rate = await ProviderFor(handler).GetRateAsync("RON", "RON");

        Assert.Equal(1m, rate);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task Successful_rate_is_reused_for_later_requests()
    {
        var handler = new StubHandler(HttpStatusCode.OK, RonToEur);
        var provider = ProviderFor(handler);

        var first = await provider.GetRateAsync("RON", "EUR");
        var firstRequest = handler.LastRequest;
        var second = await provider.GetRateAsync("ron", "eur");

        Assert.Equal(first, second);
        Assert.Equal(firstRequest, handler.LastRequest);
        Assert.Equal(1, handler.RequestCount);
    }
}