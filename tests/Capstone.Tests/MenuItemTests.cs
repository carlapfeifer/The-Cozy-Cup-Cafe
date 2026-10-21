using Capstone.Core.Models;

namespace Capstone.Tests;

public class MenuItemTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Money Ten = new(10m, "RON");

    [Fact]
    public void MenuItem_with_negative_price_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Drink ("c1", "Latte", Coffee, new Money(-1m, "RON")));
    }

    [Fact]
    public void MenuItem_with_zero_price_is_allowed()
    {
        var item = new Drink("c1", "Free water", Coffee, new Money(0m, "RON"));

        Assert.Equal(0m, item.Price.Amount);
    }

    [Fact]
    public void MenuItem_with_null_price_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Drink("c1", "Latte", Coffee, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MenuItem_with_blank_id_is_rejected(string id)
    {
        Assert.Throws<ArgumentException>(() => new Drink(id, "Latte", Coffee, Ten));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MenuItem_with_blank_display_name_is_rejected(string name)
    {
        Assert.Throws<ArgumentException>(() => new Drink("c1", name, Coffee, Ten));
    }
}