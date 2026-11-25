using Capstone.Core.Models;

namespace Capstone.Tests;

public class OrderTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Cakes = new("cake", "Cakes");
    private static DateTime At(int hour, int minute = 0) => new(2026, 10, 23, hour, minute, 0);

    private static MenuItem FoodItem(string id, decimal price) =>
        new Food(id, $"Food {id}", Cakes, new Money(price, "RON"));

    private static MenuItem DrinkItem(string id, decimal price, string currency = "RON") =>
        new Drink(id, $"Item {id}", Coffee, new Money(price, currency));

    // ───────────────────────── Totals and quantities ─────────────────────────

    // An order with no lines costs nothing, and is not an error.
    [Fact]
    public void Empty_order_has_a_total_of_zero()
    {
        var order = new Order();

        Assert.Equal(0m, order.Subtotal);
        Assert.Equal(0m, order.Total);
    }

    // Each line counts price times quantity, and the lines are added together.
    [Fact]
    public void Total_is_the_sum_of_price_times_quantity_for_every_line()
    {
        var order = new Order();
        order.Add(DrinkItem("a", 3.50m), 2); // 7.00
        order.Add(DrinkItem("b", 4.00m), 1); // 4.00

        Assert.Equal(11.00m, order.Total);
    }

    // A quantity of zero or less makes no sense, so it is rejected.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_below_one_is_rejected(int quantity)
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(DrinkItem("a", 3.50m), quantity));
    }

    // A rejected quantity must leave the order unchanged.
    [Fact]
    public void Rejected_quantity_does_not_add_a_line()
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(DrinkItem("a", 3.50m), 0));
        Assert.Empty(order.Lines);
    }

    // 0.10 + 0.20 must be exactly 0.30 (decimal, not double).
    [Fact]
    public void Prices_add_up_without_floating_point_errors()
    {
        var order = new Order(placedAt: At(12));
        order.Add(DrinkItem("a", 0.10m), 1);
        order.Add(DrinkItem("b", 0.20m), 1);

        Assert.Equal(0.30m, order.Total);
    }

    // 3 x 0.10 must be exactly 0.30 as well.
    [Fact]
    public void Quantity_multiplies_without_floating_point_errors()
    {
        var order = new Order(placedAt: At(12));
        order.Add(DrinkItem("a", 0.10m), 3);

        Assert.Equal(0.30m, order.Total);
    }

    // ───────────────────── Time and size based prices ─────────────────────

    // The order stores the time it was placed, because prices depend on it.
    [Fact]
    public void Order_remembers_when_it_was_placed()
    {
        var order = new Order(placedAt: At(12, 30));

        Assert.Equal(At(12, 30), order.PlacedAt);
    }

    // One minute before 18:00, food still costs its full price.
    [Fact]
    public void Food_ordered_before_18_00_costs_full_price()
    {
        var order = new Order(placedAt: At(17, 59));
        order.Add(FoodItem("f1", 15m), 2);

        Assert.Equal(30.00m, order.Total);
    }

    // From exactly 18:00, food is half price.
    [Fact]
    public void Food_ordered_from_18_00_costs_half_price()
    {
        var order = new Order(placedAt: At(18));
        order.Add(FoodItem("f1", 15m), 2);

        Assert.Equal(15.00m, order.Total);
    }

    // A large drink costs 20% more, whatever the time.
    [Fact]
    public void Large_drink_costs_20_percent_more_in_an_order()
    {
        var order = new Order(placedAt: At(12));
        order.Add(new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), Size.Large), 2);

        Assert.Equal(24.00m, order.Total);
    }

    // The half-price rule hits only the food line, not the whole order.
    [Fact]
    public void Time_based_price_applies_per_line_not_to_the_whole_order()
    {
        var order = new Order(placedAt: At(19));
        order.Add(DrinkItem("d1", 10m), 1); // stays 10.00
        order.Add(FoodItem("f1", 20m), 1); // 10.00 after 18:00

        Assert.Equal(20.00m, order.Total);
    }

    // A combo is neither a drink nor food, so it keeps its listed price.
    [Fact]
    public void Combo_keeps_its_listed_price_after_18_00()
    {
        var order = new Order(placedAt: At(19));
        var combo = new Combo("c1", "Breakfast", Cakes, new Money(18m, "RON"),
            [DrinkItem("d1", 10m), FoodItem("f1", 9m)]);
        order.Add(combo, 1);

        Assert.Equal(18.00m, order.Total);
    }

    // ───────────────────────────── Discounts ─────────────────────────────

    // A 10% discount takes 2.00 off 20.00.
    [Fact]
    public void Discount_is_taken_off_the_subtotal()
    {
        var order = new Order();
        order.Add(DrinkItem("a", 10.00m), 2); // 20.00
        order.ApplyDiscount(10);

        Assert.Equal(18.00m, order.Total);
    }

    // The discount comes after the time rule: 20.00 becomes 10.00, then 9.00.
    [Fact]
    public void Discount_is_applied_on_top_of_the_time_based_price()
    {
        var order = new Order(placedAt: At(19));
        order.Add(FoodItem("f1", 20m), 1); // 10.00 after 18:00
        order.ApplyDiscount(10);

        Assert.Equal(9.00m, order.Total);
    }

    // A discount below 0% or above 100% is rejected.
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Discount_outside_0_to_100_is_rejected(int percent)
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.ApplyDiscount(percent));
    }

    // Edge of the range: 100% is allowed and makes the order free.
    [Fact]
    public void Full_discount_makes_the_order_free()
    {
        var order = new Order(placedAt: At(12));
        order.Add(DrinkItem("a", 10.00m), 1);
        order.ApplyDiscount(100);

        Assert.Equal(0m, order.Total);
    }

    // Edge of the range: 0% is allowed and changes nothing.
    [Fact]
    public void Zero_discount_keeps_the_total()
    {
        var order = new Order(placedAt: At(12));
        order.Add(DrinkItem("a", 10.00m), 1);
        order.ApplyDiscount(0);

        Assert.Equal(10.00m, order.Total);
    }

    // ───────────────────────────── Rounding ─────────────────────────────

    // 0.045 rounds up to 0.05 (not to the even digit 0.04).
    [Fact]
    public void Half_a_cent_rounds_up()
    {
        var order = new Order();
        order.Add(DrinkItem("a", 0.05m), 1);
        order.ApplyDiscount(10); // 0.05 * 0.9 = 0.045

        Assert.Equal(0.05m, order.Total);
    }

    // 8.4915 is rounded to two decimals: 8.49.
    [Fact]
    public void Total_is_rounded_to_two_decimals()
    {
        var order = new Order();
        order.Add(DrinkItem("a", 9.99m), 1);
        order.ApplyDiscount(15); // 9.99 * 0.85 = 8.4915

        Assert.Equal(8.49m, order.Total);
    }

    // ───────────────────────── Items the order refuses ─────────────────────────

    // An item priced in EUR cannot go into a RON order.
    [Fact]
    public void Item_in_another_currency_is_rejected()
    {
        var order = new Order();   // RON by default

        Assert.Throws<ArgumentException>(() => order.Add(DrinkItem("a", 3.50m, "EUR"), 1));
    }

    // A sold-out item cannot be ordered.
    [Fact]
    public void Unavailable_item_is_rejected()
    {
        var order = new Order(placedAt: At(12));
        var soldOut = new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), isAvailable: false);

        Assert.Throws<InvalidOperationException>(() => order.Add(soldOut, 1));
    }

    // A rejected sold-out item must leave the order unchanged.
    [Fact]
    public void Rejected_unavailable_item_does_not_add_a_line()
    {
        var order = new Order(placedAt: At(12));
        var soldOut = new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), isAvailable: false);

        Assert.Throws<InvalidOperationException>(() => order.Add(soldOut, 1));
        Assert.Empty(order.Lines);
    }
    
    [Fact]
    public void Dine_in_vat_is_calculated_at_the_discounted_price()
    {
        var drink = new Drink(
            "c1",
            "Latte",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M",
            new VatInfo(11m, 21m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 10, 0, 0),
            ServiceMode.DineIn);

        order.Add(drink, 1);
        order.ApplyDiscount(40m);

        Assert.Equal(6m, order.Total);
        Assert.Equal(0.59m, order.VatTotal);
        Assert.Equal(0.59m, order.VatBreakdown[11m]);
        Assert.Equal(10m, order.Subtotal);
        Assert.Equal(40m, order.DiscountPercent);
        Assert.Equal(10m, drink.Price.Amount);
        Assert.Equal(Size.Medium, drink.Size);
      
    }

    [Fact]
    public void Mixed_combo_vat_is_split_by_component()
    {
        var coffee = new Drink(
            "c1",
            "Coffee",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"),
            Size.Medium,
            Allergen.None,
            true,
            false,
            true,
            5,
            "COF-001",
            new VatInfo(11m, 11m));

        var juice = new Drink(
            "cold-1",
            "Iced Juice",
            new Category("cold", "Cold Drinks"),
            new Money(10m, "RON"),
            Size.Medium,
            Allergen.None,
            true,
            false,
            false,
            5,
            "COLD-001",
            new VatInfo(11m, 21m));

        var combo = new Combo(
            "combo-1",
            "Mixed Combo",
            new Category("combo", "Combos"),
            new Money(15m, "RON"),
            new MenuItem[] { coffee, juice },
            true,
            false,
            5,
            "COMBO-001",
            new VatInfo(11m, 11m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 12, 0, 0),
            ServiceMode.Takeaway);

        order.Add(combo, 1);

        Assert.Equal(11m, Assert.Single(order.VatBreakdown, pair => pair.Key == 11m).Key);
        Assert.Equal(21m, Assert.Single(order.VatBreakdown, pair => pair.Key == 21m).Key);
    }
    
    
    [Fact]
    public void Dine_in_uses_reduced_vat()
    {
        var item = new Drink(
            "cof-1",
            "Latte",
            new Category("cof", "Coffee", 11m),
            new Money(14.50m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M",
            new VatInfo(11m, 21m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 10, 0, 0),
            ServiceMode.DineIn);

        order.Add(item, 1);

        Assert.True(order.VatBreakdown.ContainsKey(11m));
        Assert.False(order.VatBreakdown.ContainsKey(21m));
    }

    [Fact]
    public void Takeaway_uses_standard_vat_for_sugar_drinks()
    {
        var item = new Drink(
            "cof-1",
            "Latte",
            new Category("cof", "Coffee", 11m),
            new Money(14.50m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M",
            new VatInfo(11m, 21m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 10, 0, 0),
            ServiceMode.Takeaway);

        order.Add(item, 1);

        Assert.True(order.VatBreakdown.ContainsKey(21m));
    }

    [Fact]
    public void Sandwich_stays_reduced_for_takeaway()
    {
        var item = new Food(
            "san-1",
            "Ham sandwich",
            new Category("san", "Sandwiches", 11m),
            new Money(22.75m, "RON"),
            Allergen.Gluten,
            true,
            false,
            5,
            "SAN-HAM",
            new VatInfo(11m, 11m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 12, 0, 0),
            ServiceMode.Takeaway);

        order.Add(item, 1);

        Assert.True(order.VatBreakdown.ContainsKey(11m));
    }
    [Fact]
    public void Category_rejects_invalid_vat()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Category("cof", "Coffee", 101m));
    }
    [Fact]
    public void Takeaway_vat_is_calculated_at_the_takeaway_rate()
    {
        var drink = new Drink(
            "c1",
            "Latte",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M",
            new VatInfo(11m, 21m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 10, 0, 0),
            ServiceMode.Takeaway);

        order.Add(drink, 1);

        Assert.Equal(10m, order.Total);
        Assert.Equal(1.74m, order.VatTotal);
        Assert.Equal(1.74m, order.VatBreakdown[21m]);
    }
    [Fact]
    public void Discount_is_not_added_back_when_calculating_vat()
    {
        var drink = new Drink(
            "c1",
            "Latte",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M",
            new VatInfo(11m, 21m));

        var order = new Order(
            "RON",
            new DateTime(2026, 11, 24, 10, 0, 0),
            ServiceMode.DineIn);

        order.Add(drink, 1);
        order.ApplyDiscount(40m);

        Assert.Equal(10m, order.Subtotal);
        Assert.Equal(40m, order.DiscountPercent);
        Assert.Equal(6m, order.Total);
        Assert.Equal(0.59m, order.VatTotal);
    }
    
    [Fact]
    public void Quantity_zero_is_rejected()
    {
        var menuItem = DrinkItem("a", 3.50m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OrderLine(menuItem, 0));
    }

    [Fact]
    public void Negative_quantity_is_rejected()
    {
        var menuItem = DrinkItem("a", 3.50m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OrderLine(menuItem, -1));
    }

    [Fact]
    public void Discount_is_applied_once()
    {
        var order = new Order();
        order.Add(DrinkItem("a", 100m), 1);

        order.ApplyDiscount(10m);
        order.ApplyDiscount(20m);

        Assert.Equal(80m, order.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Discount_boundaries_are_valid(int percent)
    {
        var order = new Order();
        order.Add(DrinkItem("a", 25m), 1);

        order.ApplyDiscount(percent);

        Assert.Equal(
            percent == 100 ? 0m : 25m,
            order.Total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Discount_outside_the_allowed_range_is_rejected(int percent)
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => order.ApplyDiscount(percent));
    }
    
    [Fact]
    public void Order_total_uses_decimal_not_double()
    {
        var order = new Order();
        order.Add(DrinkItem("a", 0.10m), 1);
        order.Add(DrinkItem("b", 0.20m), 1);

        Assert.Equal(0.30m, order.Total);
    }
    
}