using Capstone.Core.Models;

namespace Capstone.Core;

/// <summary>The best seller of one category.</summary>
public sealed record CategoryBestSeller(string Category, string Item, int UnitsSold);

/// <summary>What the café took in during one hour of the day (0 to 23).</summary>
public sealed record HourRevenue(int Hour, decimal Revenue);

public sealed record CategoryMenuCount(
    string CategoryId,
    string CategoryName,
    int ItemCount);

public sealed record PaymentMethodRevenue(
    PaymentMethod Method,
    string Currency,
    decimal Revenue);

public sealed record MenuItemSales(
    string ItemId,
    string ItemName,
    int UnitsSold);

public sealed record OrderLineMenuItem(
    Order Order,
    MenuItem Item,
    int Quantity);

public sealed record CategoryMenuItem(
    Category Category,
    MenuItem Item);

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
    
    public static IReadOnlyList<CategoryMenuCount> MenuItemCountPerCategory(
    IEnumerable<MenuItem> menu)
{
    ArgumentNullException.ThrowIfNull(menu);

    return menu
        .GroupBy(item => item.Category.Id, StringComparer.OrdinalIgnoreCase)
        .Select(group => new CategoryMenuCount(
            group.Key,
            group.First().Category.Name,
            group.Count()))
        .OrderBy(result => result.CategoryName, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

public static IReadOnlyList<PaymentMethodRevenue> RevenueByPaymentMethod(
    IEnumerable<Order> orders)
{
    ArgumentNullException.ThrowIfNull(orders);

    return orders
        .GroupBy(order => new { order.PaymentMethod, order.Currency })
        .Select(group => new PaymentMethodRevenue(
            group.Key.PaymentMethod,
            group.Key.Currency,
            group.Sum(order => order.Total)))
        .OrderBy(result => result.Method)
        .ThenBy(result => result.Currency, StringComparer.Ordinal)
        .ToList();
}

public static IReadOnlyList<MenuItemSales> SalesCountPerMenuItem(
    IEnumerable<Order> orders)
{
    ArgumentNullException.ThrowIfNull(orders);

    return orders
        .SelectMany(order => order.Lines)
        .GroupBy(line => line.Item.Id, StringComparer.Ordinal)
        .Select(group => new MenuItemSales(
            group.Key,
            group.First().Item.DisplayName,
            group.Sum(line => line.Quantity)))
        .OrderBy(result => result.ItemId, StringComparer.Ordinal)
        .ToList();
}

public static IReadOnlyList<OrderLineMenuItem> JoinOrderLinesToMenu(
    IEnumerable<Order> orders,
    IEnumerable<MenuItem> menu)
{
    ArgumentNullException.ThrowIfNull(orders);
    ArgumentNullException.ThrowIfNull(menu);

    return orders
        .SelectMany(
            order => order.Lines,
            (order, line) => new { Order = order, Line = line })
        .Join(
            menu,
            entry => entry.Line.Item.Id,
            item => item.Id,
            (entry, item) => new OrderLineMenuItem(
                entry.Order,
                item,
                entry.Line.Quantity))
        .ToList();
}

public static IReadOnlyList<CategoryMenuItem> JoinCategoriesToMenu(
    IEnumerable<Category> categories,
    IEnumerable<MenuItem> menu)
{
    ArgumentNullException.ThrowIfNull(categories);
    ArgumentNullException.ThrowIfNull(menu);

    return categories
        .Join(
            menu,
            category => category.Id,
            item => item.Category.Id,
            (category, item) => new CategoryMenuItem(category, item))
        .ToList();
}

public static Order? BiggestOrder(IEnumerable<Order> orders)
{
    ArgumentNullException.ThrowIfNull(orders);

    return orders
        .OrderByDescending(order => order.Total)
        .ThenBy(order => order.PlacedAt)
        .FirstOrDefault();
}

public static CategoryBestSeller? FastestSellingPastryInFinalTwoHours(
    IEnumerable<Order> orders)
{
    ArgumentNullException.ThrowIfNull(orders);

    return orders
        .Where(order => order.PlacedAt.Hour is 18 or 19)
        .SelectMany(order => order.Lines)
        .Where(line => string.Equals(
            line.Item.Category.Name,
            "Pastries",
            StringComparison.OrdinalIgnoreCase))
        .GroupBy(line => line.Item.Id, StringComparer.Ordinal)
        .Select(group =>
        {
            var item = group.First().Item;
            return new CategoryBestSeller(
                item.Category.Name,
                item.DisplayName,
                group.Sum(line => line.Quantity));
        })
        .OrderByDescending(result => result.UnitsSold)
        .ThenBy(result => result.Item, StringComparer.Ordinal)
        .FirstOrDefault();
}
}