using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core;

public static class ItemQueries
{
    public static IEnumerable<IItem> Longest(this IEnumerable<IItem> items, int n)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return items
            .OrderByDescending(item => item.DisplayName.Length)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(n);
    }

    public static IEnumerable<MenuItem> AvailableItems(
        this IEnumerable<MenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Where(item => item.IsAvailable);
    }

    public static IEnumerable<MenuItem> VeganItems(
        this IEnumerable<MenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items.Where(item =>
            (item.Allergens & (Allergen.Milk | Allergen.Eggs)) == Allergen.None);
    }

    public static IEnumerable<MenuItem> ByCategory(
        this IEnumerable<MenuItem> items,
        string categoryName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        return items.Where(item =>
            string.Equals(
                item.Category.Name,
                categoryName,
                StringComparison.OrdinalIgnoreCase));
    }

    public static string ToReceiptLine(this MenuItem item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return $"{quantity}x {item.DisplayName} - " +
               $"{item.Price.Amount:0.00} {item.Price.Currency}";
    }
}