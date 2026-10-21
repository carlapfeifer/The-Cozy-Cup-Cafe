using Capstone.Core.Models;

namespace Capstone.Tests;

public class PricingTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Cakes = new("cake", "Cakes");
    private static readonly TimeOnly Noon = new(12, 0);

    [Fact]
    public void Large_drink_costs_20_percent_more()
    {
        var latte = new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), Size.Large);

        Assert.Equal(12.00m, Pricing.FinalPrice(latte, Noon));
    }

    [Fact]
    public void Medium_drink_keeps_its_price()
    {
        var latte = new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), Size.Medium);

        Assert.Equal(10.00m, Pricing.FinalPrice(latte, Noon));
    }

    [Fact]
    public void Food_before_18_00_costs_full_price()
    {
        var cake = new Food("f1", "Cheesecake", Cakes, new Money(15m, "RON"));

        Assert.Equal(15.00m, Pricing.FinalPrice(cake, new TimeOnly(17, 59)));
    }

    [Fact]
    public void Food_from_18_00_costs_half_price()
    {
        var cake = new Food("f1", "Cheesecake", Cakes, new Money(15m, "RON"));

        Assert.Equal(7.50m, Pricing.FinalPrice(cake, new TimeOnly(18, 0)));
    }
}