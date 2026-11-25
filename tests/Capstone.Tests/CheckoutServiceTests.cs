using Capstone.Core.Currency;
using Capstone.Core.Models;
using Capstone.Core.Payments;

namespace Capstone.Tests;

public class CheckoutServiceTests
{
    private sealed class FakeRates(decimal rate) : IExchangeRateProvider
    {
        public Task<decimal> GetRateAsync(
            string from,
            string to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rate);
    }

    private sealed class FakePaymentProcessor(bool approve) : IPaymentProcessor
    {
        public Money LastAmount { get; private set; } = new(0m, "RON");

        public Task<bool> ProcessPaymentAsync(
            Money amount,
            CancellationToken cancellationToken = default)
        {
            LastAmount = amount;
            return Task.FromResult(approve);
        }
    }

    [Fact]
    public async Task Approved_payment_converts_total_and_records_order()
    {
        var history = new CompletedOrderHistory();
        var processor = new FakePaymentProcessor(approve: true);
        var checkout = new CheckoutService(
            new CurrencyConverter(new FakeRates(0.2m)),
            processor,
            history);
        var order = CreateOrder();

        var result = await checkout.CheckoutAsync(order, "EUR");

        Assert.True(result.IsApproved);
        Assert.Equal(new Money(2.40m, "EUR"), result.PaymentAmount);
        Assert.Equal(result.PaymentAmount, processor.LastAmount);
        Assert.Same(order, Assert.Single(history.Orders));
    }

    [Fact]
    public async Task Declined_payment_does_not_record_order()
    {
        var history = new CompletedOrderHistory();
        var checkout = new CheckoutService(
            new CurrencyConverter(new FakeRates(0.2m)),
            new FakePaymentProcessor(approve: false),
            history);

        var result = await checkout.CheckoutAsync(CreateOrder(), "EUR");

        Assert.False(result.IsApproved);
        Assert.Empty(history.Orders);
    }

    [Fact]
    public async Task Empty_order_cannot_be_checked_out()
    {
        var checkout = new CheckoutService(
            new CurrencyConverter(new FakeRates(0.2m)),
            new FakePaymentProcessor(approve: true),
            new CompletedOrderHistory());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => checkout.CheckoutAsync(new Order(), "EUR"));
    }

    [Fact]
    public async Task An_order_cannot_be_paid_twice()
    {
        var history = new CompletedOrderHistory();
        var checkout = new CheckoutService(
            new CurrencyConverter(new FakeRates(0.2m)),
            new FakePaymentProcessor(approve: true),
            history);
        var order = CreateOrder();

        await checkout.CheckoutAsync(order, "EUR");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => checkout.CheckoutAsync(order, "EUR"));

        Assert.Single(history.Orders);
    }

    private static Order CreateOrder()
    {
        var item = new Drink(
            "checkout-drink",
            "Latte",
            new Category("cof", "Coffee"),
            new Money(12m, "RON"));

        var order = new Order();
        order.Add(item, 1);
        return order;
    }
}