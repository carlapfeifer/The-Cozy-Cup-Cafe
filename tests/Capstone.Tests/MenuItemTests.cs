using Capstone.Core.Models;

namespace Capstone.Tests;

public class MenuItemTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Money Ten = new(10m, "RON");

    [Fact]
    public void MenuItem_with_negative_price_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Drink("c1", "Latte", Coffee, new Money(-1m, "RON")));
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
        Assert.Throws<ArgumentNullException>(() => new Drink("c1", "Latte", Coffee, null!));
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

    [Fact]
    public void Featured_drink_is_created_as_featured()
    {
        var item = new Drink(
            "featured-1",
            "Featured latte",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            true);

        Assert.True(item.IsFeatured);
    }

    [Fact]
    public void Changing_availability_preserves_featured_state()
    {
        var item = new Drink(
            "featured-1",
            "Featured latte",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            true);

        var updated = item.WithAvailability(false);

        Assert.True(updated.IsFeatured);
        Assert.False(updated.IsAvailable);
    }

    [Fact]
    public void Changing_price_preserves_featured_state()
    {
        var item = new Food(
            "featured-2",
            "Featured croissant",
            Coffee,
            Ten,
            Allergen.Gluten,
            true,
            true);

        var updated = item.WithPrice(new Money(12m, "RON"));

        Assert.True(updated.IsFeatured);
        Assert.Equal(12m, updated.Price.Amount);
    }

    [Fact]
    public void Mapper_round_trips_featured_state()
    {
        var mapper = new MenuItemMapper();
        var item = new Drink(
            "featured-3",
            "Featured mocha",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            true);

        var json = mapper.ToJson(item);
        var loaded = Assert.IsType<Drink>(mapper.FromJson(json));

        Assert.True(loaded.IsFeatured);
    }

    [Theory]
    [InlineData("Fish")]
    [InlineData("Sesame")]
    [InlineData("Eggs")]
    public void Known_allergen_names_are_valid(string allergen)
    {
        Assert.True(Enum.TryParse<Allergen>(allergen, out var parsed));
        Assert.True(Enum.IsDefined(parsed));
    }

    [Fact]
    public void Cold_drink_is_not_marked_hot()
    {
        var drink = new Drink(
            "cold-1",
            "Iced Tea",
            new Category("cold", "Cold Drinks"),
            new Money(12m, "RON"),
            Size.Medium,
            Allergen.None,
            true,
            false,
            false);

        Assert.False(drink.IsHot);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_preparation_time_is_rejected(int minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Food(
                "food-1",
                "Croissant",
                new Category("pas", "Pastries"),
                new Money(8m, "RON"),
                Allergen.Gluten,
                true,
                false,
                minutes));
    }

    [Fact]
    public void Decaf_drink_is_marked_decaf()
    {
        var drink = new Drink(
            "decaf-1",
            "Decaf Latte",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-DECAF-M",
            null,
            true);

        Assert.True(drink.IsDecaf);
    }

    [Fact]
    public void Changing_decaf_drink_availability_preserves_decaf()
    {
        var drink = new Drink(
            "decaf-1",
            "Decaf Latte",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-DECAF-M",
            null,
            true);

        var updated = Assert.IsType<Drink>(drink.WithAvailability(false));

        Assert.True(updated.IsDecaf);
        Assert.False(updated.IsAvailable);
    }

    [Fact]
    public void Hot_drink_estimated_preparation_is_two_minutes()
    {
        var drink = new Drink(
            "hot-1",
            "Latte",
            Coffee,
            Ten,
            Size.Medium,
            isHot: true);

        Assert.Equal(2, drink.EstimatedPreparationMinutes);
    }

    [Fact]
    public void Cold_drink_estimated_preparation_is_one_minute()
    {
        var drink = new Drink(
            "cold-1",
            "Iced Tea",
            Coffee,
            Ten,
            Size.Medium,
            isHot: false);

        Assert.Equal(1, drink.EstimatedPreparationMinutes);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    public void Food_preparation_time_must_be_between_three_and_eight_minutes(int minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Food(
                "food-1",
                "Croissant",
                new Category("pas", "Pastries"),
                new Money(8m, "RON"),
                Allergen.Gluten,
                true,
                false,
                minutes));
    }


    [Fact]
    public void MenuItem_rejects_blank_sku()
    {
        Assert.Throws<ArgumentException>(() =>
            new Drink(
                "sku-test",
                "Latte",
                Coffee,
                Ten,
                Size.Medium,
                Allergen.Milk,
                true,
                false,
                true,
                5,
                " "));
    }

    [Fact]
    public void MenuItem_generates_sku_when_not_supplied()
    {
        var item = new Drink(
            "sku-test",
            "Latte",
            Coffee,
            Ten);

        Assert.Equal("DRINK-sku-test", item.Sku);
    }

    [Fact]
    public void Changing_availability_preserves_sku()
    {
        var item = new Drink(
            "sku-test",
            "Latte",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M");

        var updated = item.WithAvailability(false);

        Assert.Equal("COF-LAT-M", updated.Sku);
    }

    [Fact]
    public void Mapper_round_trips_sku()
    {
        var mapper = new MenuItemMapper();
        var item = new Drink(
            "sku-test",
            "Latte",
            Coffee,
            Ten,
            Size.Medium,
            Allergen.Milk,
            true,
            false,
            true,
            5,
            "COF-LAT-M");

        var loaded = Assert.IsType<Drink>(
            mapper.FromJson(mapper.ToJson(item)));

        Assert.Equal("COF-LAT-M", loaded.Sku);
    }

    [Theory]
    [InlineData("food")]
    [InlineData("drink")]
    [InlineData("combo")]
    public void Mapper_round_trips_preparation_time(string kind)
    {
        var mapper = new MenuItemMapper();
        MenuItem item = kind switch
        {
            "food" => new Food(
                "food-time",
                "Croissant",
                new Category("pas", "Pastries"),
                Ten,
                preparationTimeMinutes: 6),
            "drink" => new Drink(
                "drink-time",
                "Latte",
                Coffee,
                Ten,
                preparationTimeMinutes: 7),
            _ => new Combo(
                "combo-time",
                "Coffee and croissant",
                Coffee,
                Ten,
                new MenuItem[]
                {
                    new Drink("combo-drink", "Coffee", Coffee, Ten),
                    new Food(
                        "combo-food",
                        "Croissant",
                        new Category("pas", "Pastries"),
                        Ten)
                },
                preparationTimeMinutes: 8)
        };

        var loaded = Assert.IsAssignableFrom<MenuItem>(
            mapper.FromJson(mapper.ToJson(item)));

        Assert.Equal(item.PreparationTimeMinutes, loaded.PreparationTimeMinutes);
    }

    [Fact]
    public void Mapper_rejects_unknown_kind()
    {
        const string json = """
                            {
                              "kind": "dessert",
                              "id": "item-1",
                              "displayName": "Croissant",
                              "category": { "id": "pas", "name": "Pastries" },
                              "price": { "amount": 5, "currency": "RON" }
                            }
                            """;

        Assert.Null(new MenuItemMapper().FromJson(json));
    }


    [Fact]
    public void Duplicate_category_name_is_rejected()
    {
        var registry = new CategoryRegistry();

        registry.Add(new Category("cof", "Coffee"));

        Assert.Throws<ArgumentException>(() => registry.Add(new Category("hot-cof", "coffee")));
    }
}