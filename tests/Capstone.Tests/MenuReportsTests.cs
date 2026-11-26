using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Storage;

namespace Capstone.Tests;

public class MenuReportsTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Pastries = new("pas", "Pastries");
    private static readonly Category Salads = new("sal", "Salads");

    private static readonly MenuItem Latte = new Drink("c1", "Latte", Coffee, new Money(14m, "RON"), Size.Medium, Allergen.Milk);
    private static readonly MenuItem Espresso = new Drink("c2", "Espresso", Coffee, new Money(9m, "RON"));
    private static readonly MenuItem Croissant = new Food("p1", "Croissant", Pastries, new Money(8m, "RON"), Allergen.Gluten | Allergen.Milk);
    private static readonly MenuItem Tart = new Food("p2", "Almond tart", Pastries, new Money(12m, "RON"), Allergen.Nuts);
    private static readonly MenuItem Salad = new Food("s1", "Garden salad", Salads, new Money(16m, "RON"));

    private static readonly MenuItem[] Menu = { Latte, Espresso, Croissant, Tart, Salad };

    private static Order Placed(int hour, int minute, params (MenuItem Item, int Quantity)[] lines)
    {
        var order = new Order("RON", new DateTime(2026, 11, 19, hour, minute, 0));
        foreach (var (item, quantity) in lines)
            order.Add(item, quantity);
        return order;
    }

    // A small morning-and-lunch day. Totals: 36 + 33 + 26 + 8.10
    private static List<Order> Day()
    {
        var discounted = Placed(12, 45, (Espresso, 1));
        discounted.ApplyDiscount(10m);   // 9 -> 8.10

        return new List<Order>
        {
            Placed(9, 10, (Latte, 2), (Croissant, 1)),
            Placed(9, 50, (Espresso, 1), (Croissant, 3)),
            Placed(12, 30, (Latte, 1), (Tart, 1)),
            discounted,
        };
    }

    [Fact]
    public void Best_seller_per_category_picks_the_most_units()
    {
        var best = MenuReports.BestSellerPerCategory(Day());

        Assert.Equal(2, best.Count);
        Assert.Equal(new CategoryBestSeller("Coffee", "Latte", 3), best[0]);        // latte 3, espresso 2
        Assert.Equal(new CategoryBestSeller("Pastries", "Croissant", 4), best[1]);  // croissant 4, tart 1
    }

    [Fact]
    public void Best_seller_tie_goes_to_the_lower_id()
    {
        var orders = new List<Order> { Placed(9, 0, (Latte, 1), (Espresso, 1)) };

        var best = MenuReports.BestSellerPerCategory(orders);

        Assert.Equal("Latte", best.Single().Item);   // c1 before c2
    }

    [Fact]
    public void Revenue_by_hour_adds_up_each_hour_with_discounts()
    {
        var revenue = MenuReports.RevenueByHour(Day());

        Assert.Equal(new[] { new HourRevenue(9, 69m), new HourRevenue(12, 34.10m) }, revenue.ToArray());
    }

    [Fact]
    public void Never_ordered_lists_only_items_in_no_order()
    {
        var never = MenuReports.NeverOrdered(Menu, Day());

        Assert.Equal(new[] { "s1" }, never.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Average_order_value_is_rounded_to_the_cent()
    {
        // (36 + 33 + 26 + 8.10) / 4 = 25.775, which rounds up to 25.78
        Assert.Equal(25.78m, MenuReports.AverageOrderValue(Day()));
    }

    [Fact]
    public void Average_order_value_of_no_orders_is_zero()
    {
        Assert.Equal(0m, MenuReports.AverageOrderValue(new List<Order>()));
    }

    [Fact]
    public void Vegan_under_a_limit_excludes_milk_eggs_and_the_limit_itself()
    {
        var vegan = MenuReports.VeganUnder(Menu, 12m);   // tart costs exactly 12, so it is not "under"

        Assert.Equal(new[] { "c2" }, vegan.Select(item => item.Id).ToArray());
        Assert.Equal(new[] { "c2", "p2" }, MenuReports.VeganUnder(Menu, 15m).Select(item => item.Id).ToArray());
    }

    [Fact]
    public void With_no_orders_the_reports_are_empty()
    {
        var none = new List<Order>();

        Assert.Equal(0, MenuReports.BestSellerPerCategory(none).Count);
        Assert.Equal(0, MenuReports.RevenueByHour(none).Count);
        Assert.Equal(Menu.Length, MenuReports.NeverOrdered(Menu, none).Count);
    }

    [Fact]
    public void End_of_day_report_summarizes_daily_revenue_and_best_sellers()
    {
        var report = CafeReportFormatter.FormatEndOfDay(
            Day(),
            new DateOnly(2026, 11, 19));

        Assert.Contains("Completed orders: 4", report);
        Assert.Contains("Items sold: 10", report);
        Assert.Contains("Total revenue: 103.10 RON", report);
        Assert.Contains("Average order: 25.78 RON", report);
        Assert.Contains("MOST ORDERED ITEMS", report);
        Assert.Contains("Croissant: 4 sold", report);
        Assert.Contains("REVENUE BY PAYMENT METHOD", report);
        Assert.Contains("REVENUE BY HOUR", report);
        Assert.Contains("09:00 — 69.00 RON", report);
    }

    [Fact]
    public void End_of_day_report_does_not_count_other_business_dates()
    {
        var report = CafeReportFormatter.FormatEndOfDay(
            Day(),
            new DateOnly(2026, 11, 20));

        Assert.Contains("Completed orders: 0", report);
        Assert.Contains("No items sold.", report);
        Assert.DoesNotContain("Total revenue:", report);
    }

    [Fact]
    public void Null_arguments_are_programming_errors()
    {
        Assert.Throws<ArgumentNullException>(() => MenuReports.BestSellerPerCategory(null!));
        Assert.Throws<ArgumentNullException>(() => MenuReports.RevenueByHour(null!));
        Assert.Throws<ArgumentNullException>(() => MenuReports.NeverOrdered(null!, Day()));
        Assert.Throws<ArgumentNullException>(() => MenuReports.NeverOrdered(Menu, null!));
        Assert.Throws<ArgumentNullException>(() => MenuReports.AverageOrderValue(null!));
        Assert.Throws<ArgumentNullException>(() => MenuReports.VeganUnder(null!, 15m));
    }
    [Fact]
    public void Seeded_report_generation_is_repeatable()
    {
        var first = CafeDataGenerator.GenerateOrders(Menu, 50, 2026);
        var second = CafeDataGenerator.GenerateOrders(Menu, 50, 2026);

        Assert.Equal(first.Count, second.Count);

        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].PlacedAt, second[i].PlacedAt);
            Assert.Equal(first[i].Subtotal, second[i].Subtotal);
            Assert.Equal(first[i].DiscountPercent, second[i].DiscountPercent);
            Assert.Equal(first[i].ServiceMode, second[i].ServiceMode);
        }
    }

    [Fact]
    public void Seeded_report_generation_creates_50_orders()
    {
        var orders = CafeDataGenerator.GenerateOrders(Menu, 50, 2026);

        Assert.Equal(50, orders.Count);
        Assert.All(orders, order => Assert.NotEmpty(order.Lines));
    }

    [Fact]
    public void Seeded_report_generation_rejects_non_positive_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CafeDataGenerator.GenerateOrders(Menu, 0, 2026));
    }

    [Fact]
    public void Seeded_report_generation_rejects_null_menu()
    {
        Assert.Throws<ArgumentNullException>(
            () => CafeDataGenerator.GenerateOrders(null!, 50, 2026));
    }
}