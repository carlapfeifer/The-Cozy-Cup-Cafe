using Capstone.Core.Models;

namespace Capstone.Tests;

public class OrderTests
{
    private static readonly Category Coffee = new("cof", "Coffee");

    private static MenuItem Item(string id, decimal price, string currency = "RON") =>
        new Drink(id, $"Item {id}", Coffee, new Money(price, currency));

    [Fact]
    public void Empty_order_has_a_total_of_zero()
    {
        var order = new Order();

        Assert.Equal(0m, order.Subtotal);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void Total_is_the_sum_of_price_times_quantity_for_every_line()
    {
        var order = new Order();
        order.Add(Item("a", 3.50m), 2);   // 7.00
        order.Add(Item("b", 4.00m), 1);   // 4.00

        Assert.Equal(11.00m, order.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_below_one_is_rejected(int quantity)
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(Item("a", 3.50m), quantity));
    }

    [Fact]
    public void Rejected_quantity_does_not_add_a_line()
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(Item("a", 3.50m), 0));
        Assert.Empty(order.Lines);
    }

    [Fact]
    public void Discount_is_taken_off_the_subtotal()
    {
        var order = new Order();
        order.Add(Item("a", 10.00m), 2);   // 20.00
        order.ApplyDiscount(10);

        Assert.Equal(18.00m, order.Total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Discount_outside_0_to_100_is_rejected(int percent)
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.ApplyDiscount(percent));
    }

    [Fact]
    public void Half_a_cent_rounds_up()
    {
        var order = new Order();
        order.Add(Item("a", 0.05m), 1);
        order.ApplyDiscount(10);           // 0.05 * 0.9 = 0.045

        Assert.Equal(0.05m, order.Total);
    }

    [Fact]
    public void Total_is_rounded_to_two_decimals()
    {
        var order = new Order();
        order.Add(Item("a", 9.99m), 1);
        order.ApplyDiscount(15);           // 9.99 * 0.85 = 8.4915

        Assert.Equal(8.49m, order.Total);
    }

    [Fact]
    public void Item_in_another_currency_is_rejected()
    {
        var order = new Order("RON");

        Assert.Throws<ArgumentException>(() => order.Add(Item("a", 3.50m, "EUR"), 1));
    }
}