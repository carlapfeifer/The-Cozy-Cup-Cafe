using Capstone.Core.Currency;
using Capstone.Core.Models;

namespace Capstone.Tests;

public class CurrencyConverterTests
{
    private sealed class FakeRates(decimal rate) : IExchangeRateProvider
    {
        public int Calls { get; private set; }

        public Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(rate);
        }
    }

    [Fact]
    public async Task Amount_is_multiplied_by_the_rate()
    {
        var converter = new CurrencyConverter(new FakeRates(0.2m));

        var result = await converter.ConvertAsync(new Money(10m, "RON"), "EUR");

        Assert.Equal(new Money(2.00m, "EUR"), result);
    }

    [Fact]
    public async Task Half_a_cent_rounds_up()
    {
        var converter = new CurrencyConverter(new FakeRates(0.5m));

        var result = await converter.ConvertAsync(new Money(1.05m, "RON"), "EUR");   // 0.525

        Assert.Equal(0.53m, result.Amount);
    }

    [Fact]
    public async Task Same_currency_is_returned_without_asking_for_a_rate()
    {
        var rates = new FakeRates(0.2m);
        var converter = new CurrencyConverter(rates);

        var result = await converter.ConvertAsync(new Money(10m, "RON"), "ron");

        Assert.Equal(10m, result.Amount);
        Assert.Equal(0, rates.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Rate_that_is_not_positive_is_rejected(int rate)
    {
        var converter = new CurrencyConverter(new FakeRates(rate));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => converter.ConvertAsync(new Money(10m, "RON"), "EUR"));
    }

    [Fact]
    public async Task Blank_target_currency_is_rejected()
    {
        var converter = new CurrencyConverter(new FakeRates(0.2m));

        await Assert.ThrowsAsync<ArgumentException>(
            () => converter.ConvertAsync(new Money(10m, "RON"), "  "));
    }
    
    [Fact]
    public async Task Order_total_is_converted_after_the_discount()
    {
        var order = new Order();
        order.Add(new Drink("d1", "Latte", new Category("cof", "Coffee"), new Money(10m, "RON")), 2);   // 20.00
        order.ApplyDiscount(10);                                                                        // 18.00
        var converter = new CurrencyConverter(new FakeRates(0.2m));

        var result = await converter.ConvertTotalAsync(order, "EUR");

        Assert.Equal(new Money(3.60m, "EUR"), result);
    }

    [Fact]
    public async Task Converting_an_order_does_not_change_the_order()
    {
        var order = new Order();
        order.Add(new Drink("d1", "Latte", new Category("cof", "Coffee"), new Money(10m, "RON")), 1);
        var converter = new CurrencyConverter(new FakeRates(0.2m));

        await converter.ConvertTotalAsync(order, "EUR");

        Assert.Equal("RON", order.Currency);
        Assert.Equal(10.00m, order.Total);
    }
}