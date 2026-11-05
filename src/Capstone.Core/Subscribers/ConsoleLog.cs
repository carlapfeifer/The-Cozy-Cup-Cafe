using Capstone.Core.Models;

namespace Capstone.Core.Subscribers;

/// <summary>Writes a line to the console for every change in the store.</summary>
public static class ConsoleLog
{
    public static void Attach(CafeStore store)
    {
        store.ItemAddedWithDetails += (_, change) =>
            Console.WriteLine($"Added {change.Item.DisplayName}; store now has {change.TotalCount} items.");

        store.ItemRemoved += item =>
            Console.WriteLine($"Removed {item.DisplayName}.");

        store.ItemUpdated += (_, change) =>
        {
            if (change.PreviousItem.DisplayName != change.UpdatedItem.DisplayName)
                Console.WriteLine($"Renamed {change.PreviousItem.DisplayName} to {change.UpdatedItem.DisplayName}.");
        };

        store.AvailabilityChanged += (_, change) =>
            Console.WriteLine($"{change.Item.DisplayName} is now {(change.IsAvailable ? "available" : "unavailable")}.");

        store.PriceChanged += (_, change) =>
            Console.WriteLine(
                $"Changed {change.Item.DisplayName} price from {change.PreviousPrice.Amount:0.00} {change.PreviousPrice.Currency} to {change.Price.Amount:0.00} {change.Price.Currency}.");
    }
}