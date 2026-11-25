using Capstone.Core.Models;

namespace Capstone.Core;

public static class CafeDataGenerator
{
    public static IReadOnlyList<Order> GenerateOrders(
        IReadOnlyList<MenuItem> menu,
        int count,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var random = new Random(seed);
        var availableItems = menu.Where(item => item.IsAvailable).ToList();

        if (availableItems.Count == 0)
            throw new ArgumentException(
                "The menu must contain at least one available item.",
                nameof(menu));

        var paymentMethods = Enum.GetValues<PaymentMethod>();
        var orders = new List<Order>(count);

        for (var i = 0; i < count; i++)
        {
            // Cycle through 08:00–19:00 so the final two hours are represented.
            var hour = 8 + i % 12;
            var minute = random.Next(0, 60);
            var serviceMode = random.Next(2) == 0
                ? ServiceMode.DineIn
                : ServiceMode.Takeaway;
            var paymentMethod = paymentMethods[i % paymentMethods.Length];

            var order = new Order(
                "RON",
                new DateTime(2026, 11, 19, hour, minute, 0),
                serviceMode,
                paymentMethod);

            var lineCount = random.Next(1, 4);

            for (var line = 0; line < lineCount; line++)
            {
                var item = availableItems[random.Next(availableItems.Count)];
                order.Add(item, random.Next(1, 4));
            }

            // Guarantee both discounted and undiscounted orders in any multi-order report.
            if (i % 2 == 0)
                order.ApplyDiscount(random.Next(5, 21));

            orders.Add(order);
        }

        return orders;
    }
}