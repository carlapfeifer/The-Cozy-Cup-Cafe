using Capstone.Core.Currency;
using Capstone.Core.Models;

namespace Capstone.Tests;

public class CurrencyConverterTests
{
    private sealed class FakeRates(decimal rate) : IExchangeRateProvider
    {
        public int Calls { get; private set; }
        public List<(string From, string To)> Requests { get; } = new();

        public Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
        {
            Calls++;
            Requests.Add((from, to));
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

        var result = await converter.ConvertAsync(new Money(1.05m, "RON"), "EUR"); // 0.525

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

        await Assert.ThrowsAsync<InvalidOperationException>(() => converter.ConvertAsync(new Money(10m, "RON"), "EUR"));
    }

    [Fact]
    public async Task Blank_target_currency_is_rejected()
    {
        var converter = new CurrencyConverter(new FakeRates(0.2m));

        await Assert.ThrowsAsync<ArgumentException>(() => converter.ConvertAsync(new Money(10m, "RON"), "  "));
    }

    [Fact]
    public async Task Order_total_is_converted_after_the_discount()
    {
        var order = new Order();
        order.Add(new Drink("d1", "Latte", new Category("cof", "Coffee"), new Money(10m, "RON")), 2); // 20.00
        order.ApplyDiscount(10); // 18.00
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

    [Fact]
    public async Task Menu_prices_use_one_rate_per_source_currency_and_keep_menu_order()
    {
        var rates = new FakeRates(0.20m);
        var converter = new CurrencyConverter(rates);
        var coffee = new Category("cof", "Coffee");
        var items = new MenuItem[]
        {
            new Drink("d1", "Latte", coffee, new Money(12m, "RON")),
            new Food("f1", "Croissant", new Category("pas", "Pastries"), new Money(8m, "RON"))
        };

        var prices = await converter.ConvertMenuPricesAsync(items, "EUR");

        Assert.Equal(1, rates.Calls);
        Assert.Equal(("RON", "EUR"), Assert.Single(rates.Requests));
        Assert.Equal(new[] { "Latte", "Croissant" }, prices.Select(price => price.Item.DisplayName));
        Assert.Equal(new[] { 2.40m, 1.60m }, prices.Select(price => price.Price.Amount));
        Assert.All(prices, price => Assert.Equal("EUR", price.Price.Currency));
        Assert.Equal(12m, items[0].Price.Amount);
    }


    [Fact]
    public async Task RON_is_returned_without_conversion()
    {
        var converter = new CurrencyConverter(new FakeRates(99m));

        var result = await converter.ConvertAsync(
            new Money(12.50m, "RON"),
            "RON");

        Assert.Equal(12.50m, result.Amount);
        Assert.Equal("RON", result.Currency);
    }

    [Fact]
    public async Task RON_is_converted_to_EUR()
    {
        var converter = new CurrencyConverter(new FakeRates(0.20m));

        var result = await converter.ConvertAsync(
            new Money(10m, "RON"),
            "EUR");

        Assert.Equal(new Money(2m, "EUR"), result);
    }

    [Fact]
    public async Task RON_is_converted_to_USD()
    {
        var converter = new CurrencyConverter(new FakeRates(0.22m));

        var result = await converter.ConvertAsync(
            new Money(10m, "RON"),
            "USD");

        Assert.Equal(new Money(2.20m, "USD"), result);
    }

    [Fact]
    public async Task Converted_total_is_rounded_to_two_decimals()
    {
        var converter = new CurrencyConverter(new FakeRates(0.20m));

        var result = await converter.ConvertAsync(
            new Money(1.055m, "RON"),
            "EUR");

        Assert.Equal(0.21m, result.Amount);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("TEST")]
    public async Task Unsupported_target_currency_is_rejected(string currency)
    {
        var converter = new CurrencyConverter(new FakeRates(0.20m));

        await Assert.ThrowsAsync<ArgumentException>(() => converter.ConvertAsync(
            new Money(10m, "RON"),
            currency));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-10)]
    public void Negative_money_is_rejected(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "RON"));
    }

    [Fact]
    public async Task Currency_conversion_does_not_change_VAT_breakdown()
    {
        var drink = new Drink(
            "vat-currency",
            "Latte",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M",
            new VatInfo(11m, 21m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 10, 0, 0),
            ServiceMode.DineIn);

        order.Add(drink, 1);
        order.ApplyDiscount(40m);

        var vatBefore = order.VatBreakdown.ToDictionary(
            pair => pair.Key,
            pair => pair.Value);

        var converted = await new CurrencyConverter(
                new FakeRates(0.20m))
            .ConvertTotalAsync(order, "EUR");

        Assert.Equal("RON", order.Currency);
        Assert.Equal("EUR", converted.Currency);
        Assert.Equal(vatBefore, order.VatBreakdown);
    }
}