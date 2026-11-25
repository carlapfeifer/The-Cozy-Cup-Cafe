using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Wiring;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class Lab07Tests
{
    
    private readonly Lab07Wiring _wiring = new();

    private static IItem Make(string id, string name) =>
        new Drink(id, name, Coffee, new Money(10m, "RON"));

    private static List<IItem> Items(params (string Id, string Name)[] values) =>
        values.Select(value => Make(value.Id, value.Name)).ToList();
    
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Pastries = new("pas", "Pastries");
    private static readonly Category Tea = new("tea", "Tea");
    private static readonly Category Sandwiches = new("san", "Sandwiches");
    private static readonly Category ColdDrinks = new("cold", "Cold Drinks");

    private static readonly MenuItem Latte =
        new Drink("latte", "Latte", Coffee, new Money(12m, "RON"), allergens: Allergen.Milk);

    private static readonly MenuItem Croissant =
        new Food("croissant", "Croissant", Pastries, new Money(8m, "RON"), Allergen.Gluten | Allergen.Milk);

    private static readonly MenuItem TeaDrink =
        new Drink("tea", "Green Tea", Tea, new Money(7m, "RON"));

    private static readonly MenuItem Sandwich =
        new Food("sandwich", "Veggie Sandwich", Sandwiches, new Money(11m, "RON"));

    private static readonly MenuItem ColdDrink =
        new Drink("cold", "Iced Tea", ColdDrinks, new Money(6m, "RON"), isAvailable: false);

    [Fact]
    public void Names_is_lazy_and_includes_items_added_after_the_query_is_created()
    {
        var store = new CafeStore();
        store.Add(Make("a", "Latte"));

        var names = _wiring.Names(store);
        store.Add(Make("b", "Mocha"));

        Assert.Equal(new[] { "Latte", "Mocha" }, names);
    }

    [Fact]
    public void Names_reflects_removals_and_can_be_enumerated_twice()
    {
        var store = new CafeStore();
        store.Add(Make("a", "Latte"));
        store.Add(Make("b", "Mocha"));

        var names = _wiring.Names(store);
        store.Remove("a");

        Assert.Equal(new[] { "Mocha" }, names);
        Assert.Equal(new[] { "Mocha" }, names);
    }

    [Fact]
    public void Sorted_ignores_case_and_breaks_name_ties_by_id()
    {
        var items = Items(
            ("z", "zebra"),
            ("m", "Mango"),
            ("b", "apple"),
            ("a", "APPLE"));

        var result = _wiring.Sorted(items);

        Assert.Equal(new[] { "a", "b", "m", "z" }, result.Select(item => item.Id));
        Assert.Equal(new[] { "z", "m", "b", "a" }, items.Select(item => item.Id));
    }

    [Fact]
    public void Page_orders_before_skipping_and_taking()
    {
        var items = Items(
            ("z", "zebra"),
            ("m", "Mango"),
            ("b", "apple"),
            ("a", "APPLE"));

        var result = _wiring.Page(items, skip: 1, take: 2);

        Assert.Equal(new[] { "b", "m" }, result.Select(item => item.Id));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public void Page_rejects_negative_arguments(int skip, int take)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _wiring.Page(Items(("a", "Latte")), skip, take));
    }

    [Fact]
    public void Longest_orders_by_name_length_then_id()
    {
        var items = Items(
            ("z", "cinnamon"),
            ("c", "croissant"),
            ("t", "tea"),
            ("a", "espresso"));

        Assert.Equal(
            new[] { "c", "a", "z" },
            _wiring.Longest(items, 3).Select(item => item.Id));

        Assert.Equal(
            new[] { "c", "a", "z" },
            items.Longest(3).Select(item => item.Id));
    }

    [Fact]
    public void Longest_rejects_negative_counts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _wiring.Longest(Items(("a", "Latte")), -1));
    }

    [Fact]
    public void Summary_counts_initials_and_calculates_name_statistics()
    {
        var items = Items(
            ("1", "Latte"),
            ("2", "latte"),
            ("3", "Bun"),
            ("4", "Croissant"));

        var summary = _wiring.Summary(items);

        Assert.Equal(4, summary.Count);
        Assert.Equal(3, summary.DistinctInitials);
        Assert.Equal("Croissant", summary.LongestName);
        Assert.Equal(5.5, summary.AverageNameLength);
    }

    [Fact]
    public void Summary_returns_zero_values_for_an_empty_sequence()
    {
        var summary = _wiring.Summary([]);

        Assert.Equal(new StoreStats(0, 0, null, 0), summary);
    }

    [Fact]
    public void Queries_reject_null_sequences()
    {
        Assert.Throws<ArgumentNullException>(() => _wiring.Names(null!));
        Assert.Throws<ArgumentNullException>(() => _wiring.Sorted(null!));
        Assert.Throws<ArgumentNullException>(() => _wiring.Page(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => _wiring.Longest(null!, 1));
        Assert.Throws<ArgumentNullException>(() => _wiring.Summary(null!));
    }
    
    
     private static Order MakeOrder(
        int hour,
        PaymentMethod method,
        params (MenuItem Item, int Quantity)[] lines)
    {
        var order = new Order(
            "RON",
            new DateTime(2026, 11, 19, hour, 0, 0),
            ServiceMode.DineIn,
            method);

        foreach (var (item, quantity) in lines)
            order.Add(item, quantity);

        return order;
    }

    [Fact]
    public void AvailableItems_filters_unavailable_items()
    {
        MenuItem[] menu = { Latte, ColdDrink };

        Assert.Equal(
            new[] { "latte" },
            menu.AvailableItems().Select(item => item.Id));
    }

    [Fact]
    public void VeganItems_excludes_milk_and_egg_allergens()
    {
        MenuItem[] menu = { Latte, Croissant, TeaDrink, Sandwich };

        Assert.Equal(
            new[] { "sandwich", "tea" },
            menu.VeganItems()
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .Select(item => item.Id));
    }

    [Fact]
    public void ByCategory_matches_category_name_without_case_sensitivity()
    {
        MenuItem[] menu = { Latte, Croissant, TeaDrink };

        Assert.Equal(
            new[] { "croissant" },
            menu.ByCategory("PASTRIES").Select(item => item.Id));
    }

    [Fact]
    public void ToReceiptLine_formats_quantity_name_and_price()
    {
        Assert.Equal("2x Latte - 12.00 RON", Latte.ToReceiptLine(2));
    }

    [Fact]
    public void MenuItemCountPerCategory_groups_menu_items()
    {
        MenuItem[] menu = { Latte, Croissant, TeaDrink, Sandwich };

        var result = MenuReports.MenuItemCountPerCategory(menu);

        Assert.Equal(4, result.Sum(category => category.ItemCount));
        Assert.Equal(1, result.Single(category => category.CategoryId == "cof").ItemCount);
        Assert.Equal(1, result.Single(category => category.CategoryId == "pas").ItemCount);
    }

    [Fact]
    public void RevenueByPaymentMethod_groups_revenue_by_method_and_currency()
    {
        var orders = new[]
        {
            MakeOrder(9, PaymentMethod.Cash, (Latte, 1)),
            MakeOrder(10, PaymentMethod.Cash, (Latte, 2)),
            MakeOrder(11, PaymentMethod.Card, (Croissant, 1))
        };

        var revenue = MenuReports.RevenueByPaymentMethod(orders);

        Assert.Equal(
            new PaymentMethodRevenue(PaymentMethod.Cash, "RON", 36m),
            revenue.Single(row => row.Method == PaymentMethod.Cash));
        Assert.Equal(
            new PaymentMethodRevenue(PaymentMethod.Card, "RON", 8m),
            revenue.Single(row => row.Method == PaymentMethod.Card));
    }

    [Fact]
    public void SalesCountPerMenuItem_sums_quantities_across_orders()
    {
        var orders = new[]
        {
            MakeOrder(9, PaymentMethod.Cash, (Latte, 2), (Croissant, 1)),
            MakeOrder(10, PaymentMethod.Card, (Latte, 3))
        };

        var sales = MenuReports.SalesCountPerMenuItem(orders);

        Assert.Equal(5, sales.Single(item => item.ItemId == "latte").UnitsSold);
        Assert.Equal(1, sales.Single(item => item.ItemId == "croissant").UnitsSold);
    }

    [Fact]
    public void JoinOrderLinesToMenu_matches_order_lines_to_menu_items_by_id()
    {
        var orders = new[] { MakeOrder(9, PaymentMethod.Cash, (Latte, 2)) };
        MenuItem[] menu = { Latte, Croissant };

        var result = MenuReports.JoinOrderLinesToMenu(orders, menu);

        var match = Assert.Single(result);
        Assert.Same(Latte, match.Item);
        Assert.Equal(2, match.Quantity);
    }

    [Fact]
    public void JoinCategoriesToMenu_matches_category_ids()
    {
        MenuItem[] menu = { Latte, Croissant };
        Category[] categories = { Coffee, Pastries, Tea };

        var result = MenuReports.JoinCategoriesToMenu(categories, menu);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, row => row.Category.Id == "cof" && row.Item.Id == "latte");
        Assert.Contains(result, row => row.Category.Id == "pas" && row.Item.Id == "croissant");
    }

    [Fact]
    public void BiggestOrder_returns_the_order_with_the_highest_total()
    {
        var small = MakeOrder(9, PaymentMethod.Cash, (Croissant, 1));
        var large = MakeOrder(10, PaymentMethod.Card, (Latte, 3));

        Assert.Same(large, MenuReports.BiggestOrder(new[] { small, large }));
        Assert.Null(MenuReports.BiggestOrder(Array.Empty<Order>()));
    }

    [Fact]
    public void FastestSellingPastry_counts_only_sales_during_18_and_19()
    {
        var beforeFinalHours = MakeOrder(17, PaymentMethod.Cash, (Croissant, 20));
        var eveningOne = MakeOrder(18, PaymentMethod.Card, (Croissant, 2));
        var eveningTwo = MakeOrder(19, PaymentMethod.Cash, (Croissant, 3));

        var result = MenuReports.FastestSellingPastryInFinalTwoHours(
            new[] { beforeFinalHours, eveningOne, eveningTwo });

        Assert.Equal(
            new CategoryBestSeller("Pastries", "Croissant", 5),
            result);
    }

    [Fact]
    public void Generator_creates_realistic_seeded_orders()
    {
        MenuItem[] menu =
        {
            Latte,
            Croissant,
            TeaDrink,
            Sandwich,
            ColdDrink,
            new Food("salad", "Garden Salad", new Category("sal", "Salads"), new Money(13m, "RON"))
        };

        var orders = CafeDataGenerator.GenerateOrders(menu, 50, seed: 2026);

        Assert.Equal(50, orders.Count);
        Assert.True(menu.Select(item => item.Category.Id).Distinct().Count() >= 5);
        Assert.Contains(orders, order => order.DiscountPercent > 0);
        Assert.Contains(orders, order => order.DiscountPercent == 0);
        Assert.Contains(orders, order => order.PlacedAt.Hour is 18 or 19);
        Assert.Contains(orders, order => order.PaymentMethod == PaymentMethod.Cash);
        Assert.Contains(orders, order => order.PaymentMethod == PaymentMethod.Card);
        Assert.Contains(orders, order => order.PaymentMethod == PaymentMethod.MobileWallet);
        Assert.Contains(menu, item => !item.IsAvailable);
        Assert.All(
            orders.SelectMany(order => order.Lines),
            line => Assert.True(line.Item.IsAvailable));
    }
}