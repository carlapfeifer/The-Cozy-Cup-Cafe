using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Wiring;

namespace Capstone.Tests;

public class ComboTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Cakes = new("cake", "Cakes");

    private static Combo Breakfast() => new(
        "combo-1", "Breakfast", Cakes, new Money(18m, "RON"),
        new MenuItem[]
        {
            new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), Size.Medium, Allergen.Milk),
            new Food("f1", "Croissant", Cakes, new Money(9m, "RON"), Allergen.Gluten | Allergen.Eggs),
        });

    [Fact]
    public void Combo_collects_the_allergens_of_its_parts()
    {
        Assert.Equal(Allergen.Milk | Allergen.Gluten | Allergen.Eggs, Breakfast().Allergens);
    }

    [Fact]
    public void Combo_without_parts_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Combo("c1", "Empty", Cakes, new Money(5m, "RON"), Array.Empty<MenuItem>()));
    }

    [Fact]
    public void Combo_describes_its_parts()
    {
        var text = Breakfast().Describe();

        Assert.Contains("Latte", text);
        Assert.Contains("Croissant", text);
    }

    [Fact]
    public void Classify_labels_a_combo_as_combo()
    {
        Assert.Equal("combo", new Lab04Wiring().Classify(Breakfast()));
    }

    [Fact]
    public void DescribeAll_describes_a_combo_without_knowing_its_kind()
    {
        var store = new CafeStore();
        var combo = Breakfast();
        store.Add(combo);

        var lines = new Lab04Wiring().DescribeAll(store);

        Assert.Equal(combo.Describe(), Assert.Single(lines));
    }
}