using Capstone.Core.Models;

namespace Capstone.Core.Payments;

/// <summary>Simulates payment approval; it never charges a real account.</summary>
public sealed class DemoPaymentProcessor : IPaymentProcessor
{
    private readonly bool _approvePayments;

    public DemoPaymentProcessor(bool approvePayments = true)
    {
        _approvePayments = approvePayments;
    }

    public Task<bool> ProcessPaymentAsync(
        Money amount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(amount);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_approvePayments);
    }
}