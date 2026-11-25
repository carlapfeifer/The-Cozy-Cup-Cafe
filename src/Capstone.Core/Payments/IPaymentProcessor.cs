using Capstone.Core.Models;

namespace Capstone.Core.Payments;

public interface IPaymentProcessor
{
    Task<bool> ProcessPaymentAsync(
        Money amount,
        CancellationToken cancellationToken = default);
}