using Capstone.Core.Currency;
using Capstone.Core.Models;

namespace Capstone.Core.Payments;

public sealed record CheckoutResult(bool IsApproved, Money PaymentAmount);

public sealed class CheckoutService
{
    private readonly CurrencyConverter _converter;
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly CompletedOrderHistory _completedOrders;
    private readonly HashSet<Order> _paidOrders = new();

    public CheckoutService(
        CurrencyConverter converter,
        IPaymentProcessor paymentProcessor,
        CompletedOrderHistory completedOrders)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(paymentProcessor);
        ArgumentNullException.ThrowIfNull(completedOrders);

        _converter = converter;
        _paymentProcessor = paymentProcessor;
        _completedOrders = completedOrders;
    }

    public async Task<CheckoutResult> CheckoutAsync(
        Order order,
        string paymentCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentCurrency);

        if (order.Lines.Count == 0)
            throw new InvalidOperationException("An empty order cannot be checked out.");

        if (_paidOrders.Contains(order))
            throw new InvalidOperationException("This order has already been paid.");

        var amount = await _converter.ConvertTotalAsync(
            order,
            paymentCurrency,
            cancellationToken);

        var approved = await _paymentProcessor.ProcessPaymentAsync(
            amount,
            cancellationToken);

        if (approved)
        {
            _paidOrders.Add(order);
            _completedOrders.Record(order);
        }

        return new CheckoutResult(approved, amount);
    }
}