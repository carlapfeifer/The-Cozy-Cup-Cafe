using Capstone.Core.Models;

namespace Capstone.Core;

/// <summary>The best seller of one category.</summary>
public sealed record CategoryBestSeller(string Category, string Item, int UnitsSold);

/// <summary>What the café took in during one hour of the day (0 to 23).</summary>
public sealed record HourRevenue(int Hour, decimal Revenue);

/// <summary>
/// Five questions the café owner asks, each answered with one LINQ query.
/// The logic lives here so the tests can reach it; Program.cs only prints the answers.
/// </summary>
public static class MenuReports
{
    /// <summary>
    /// For each category, the item with the most units sold across all orders.
    /// A tie goes to the lower item id. Categories are listed by name; a category nobody ordered from is not listed.
    /// </summary>
    public static IReadOnlyList<CategoryBestSeller> BestSellerPerCategory(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .SelectMany(order => order.Lines)
            .GroupBy(line => line.Item.Id)
            .Select(sold => new { Item = sold.First().Item, Units = sold.Sum(line => line.Quantity) })
            .GroupBy(entry => entry.Item.Category.Id)
            .Select(category => category
                .OrderByDescending(entry => entry.Units)
                .ThenBy(entry => entry.Item.Id, StringComparer.Ordinal)
                .First())
            .OrderBy(best => best.Item.Category.Name, StringComparer.Ordinal)
            .Select(best => new CategoryBestSeller(best.Item.Category.Name, best.Item.DisplayName, best.Units))
            .ToList();
    }

    /// <summary>The total taken per hour of the day (discounts included), earliest hour first.</summary>
    public static IReadOnlyList<HourRevenue> RevenueByHour(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .GroupBy(order => order.PlacedAt.Hour)
            .OrderBy(hour => hour.Key)
            .Select(hour => new HourRevenue(hour.Key, hour.Sum(order => order.Total)))
            .ToList();
    }

    /// <summary>Menu items that appear in no order line, in menu order.</summary>
    public static IReadOnlyList<MenuItem> NeverOrdered(IEnumerable<MenuItem> menu, IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(orders);

        var orderedIds = orders
            .SelectMany(order => order.Lines)
            .Select(line => line.Item.Id)
            .ToHashSet(StringComparer.Ordinal);

        return menu.Where(item => !orderedIds.Contains(item.Id)).ToList();
    }

    /// <summary>The average amount to pay per order, rounded to the cent. 0 when there are no orders.</summary>
    public static decimal AverageOrderValue(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        var totals = orders.Select(order => order.Total).ToList();
        return totals.Count == 0
            ? 0m
            : Math.Round(totals.Average(), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Vegan items costing less than <paramref name="limit"/>, in menu order.
    /// "Vegan" here means no milk and no eggs among the allergens (the menu has no vegan flag).
    /// </summary>
    public static IReadOnlyList<MenuItem> VeganUnder(IEnumerable<MenuItem> menu, decimal limit)
    {
        ArgumentNullException.ThrowIfNull(menu);

        return menu
            .Where(item => (item.Allergens & (Allergen.Milk | Allergen.Eggs)) == Allergen.None)
            .Where(item => item.Price.Amount < limit)
            .ToList();
    }
}