using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Wiring;

namespace Capstone.Tests;

public class ComboTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Cakes = new("cake", "Cakes");

    private static Combo Breakfast(bool latteAvailable = true, bool comboAvailable = true) => new(
        "combo-1", "Breakfast", Cakes, new Money(18m, "RON"),
        [
            new Drink("d1", "Latte", Coffee, new Money(10m, "RON"), Size.Medium, Allergen.Milk, latteAvailable),
            new Food("f1", "Croissant", Cakes, new Money(9m, "RON"), Allergen.Gluten | Allergen.Eggs),
        ],
        comboAvailable);

    // ───────────────────────── Building a combo ─────────────────────────

    // A combo contains every allergen of its parts, combined.
    [Fact]
    public void Combo_collects_the_allergens_of_its_parts()
    {
        Assert.Equal(Allergen.Milk | Allergen.Gluten | Allergen.Eggs, Breakfast().Allergens);
    }

    // A combo made of nothing makes no sense, so it is rejected.
    [Fact]
    public void Combo_without_parts_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Combo("c1", "Empty", Cakes, new Money(5m, "RON"), Array.Empty<MenuItem>()));
    }

    // ─────────────────── Describing and classifying ───────────────────

    // The combo's own description lists the names of its parts.
    [Fact]
    public void Combo_describes_its_parts()
    {
        var text = Breakfast().Describe();

        Assert.Contains("Latte", text);
        Assert.Contains("Croissant", text);
    }

    // The switch expression in Classify has an arm for combos.
    [Fact]
    public void Classify_labels_a_combo_as_combo()
    {
        Assert.Equal("combo", new Lab04Wiring().Classify(Breakfast()));
    }

    // The polymorphic loop handles a combo without knowing its kind.
    [Fact]
    public void DescribeAll_describes_a_combo_without_knowing_its_kind()
    {
        var store = new CafeStore();
        var combo = Breakfast();
        store.Add(combo);

        var lines = new Lab04Wiring().DescribeAll(store);

        Assert.Equal(combo.Describe(), Assert.Single(lines));
    }

    // ───────────────────────────── Availability ─────────────────────────────

    // When every part can be sold, so can the combo.
    [Fact]
    public void Combo_is_available_when_all_its_parts_are()
    {
        Assert.True(Breakfast().IsAvailable, "all parts are available, so the combo should be too");
    }

    // One sold-out part makes the whole combo unavailable.
    [Fact]
    public void Combo_is_unavailable_when_one_part_is_unavailable()
    {
        var combo = Breakfast(latteAvailable: false);

        Assert.False(combo.IsAvailable, "the latte is sold out, so the breakfast combo cannot be sold");
    }

    // Switching the combo off by hand wins over its available parts.
    [Fact]
    public void Combo_marked_unavailable_stays_unavailable_even_with_available_parts()
    {
        var combo = Breakfast(comboAvailable: false);

        Assert.False(combo.IsAvailable, "an explicit 'not available' must not be overridden by the parts");
    }

    // An order refuses a combo that has a sold-out part.
    [Fact]
    public void Order_rejects_a_combo_with_a_sold_out_part()
    {
        var order = new Order(placedAt: new DateTime(2026, 10, 23, 12, 0, 0));

        Assert.Throws<InvalidOperationException>(() => order.Add(Breakfast(latteAvailable: false), 1));
    }

    [Fact]
    public void Combo_estimated_preparation_is_the_sum_of_its_parts()
    {
        Assert.Equal(7, Breakfast().EstimatedPreparationMinutes);
    }

    [Fact]
    public void Large_combo_preparation_switch_is_used()
    {
        var drink = new Drink(
            "d1",
            "Latte",
            Coffee,
            new Money(10m, "RON"),
            Size.Medium);
        var food = new Food(
            "f1",
            "Croissant",
            Cakes,
            new Money(9m, "RON"),
            Allergen.Gluten);
        var combo = new Combo(
            "combo-large",
            "Large Breakfast",
            Cakes,
            new Money(25m, "RON"),
            new MenuItem[] { drink, food, food });

        Assert.Contains("large combo", new Lab04Wiring().PrepareOrder(combo));
    }
    [Fact]
public void Mapper_round_trips_combo()
{
    var coffee = new Category("cof", "Coffee");
    var pastry = new Category("pas", "Pastries");
    var mapper = new MenuItemMapper();

    var combo = new Combo(
        "combo-1",
        "Breakfast Combo",
        new Category("combo", "Combos"),
        new Money(24m, "RON"),
        new MenuItem[]
        {
            new Drink(
                "cof-1",
                "Latte",
                coffee,
                new Money(14m, "RON"),
                Size.Medium,
                Allergen.Milk),
            new Food(
                "pas-1",
                "Croissant",
                pastry,
                new Money(8m, "RON"),
                Allergen.Gluten)
        },
        true,
        true);

    var json = mapper.ToJson(combo);
    var loaded = Assert.IsType<Combo>(mapper.FromJson(json));

    Assert.Equal(combo.Id, loaded.Id);
    Assert.Equal(combo.DisplayName, loaded.DisplayName);
    Assert.True(loaded.IsFeatured);
    Assert.Equal(2, loaded.Parts.Count);
}

[Fact]
public void Mapper_rejects_combo_without_parts()
{
    var mapper = new MenuItemMapper();

    var json = """
    {
      "id": "combo-empty",
      "displayName": "Empty Combo",
      "kind": "combo",
      "category": { "id": "combo", "name": "Combos" },
      "price": { "amount": 20, "currency": "RON" },
      "allergens": [],
      "isAvailable": true,
      "parts": []
    }
    """;

    Assert.Null(mapper.FromJson(json));
}

[Fact]
public void Mapper_rejects_combo_with_invalid_part()
{
    var mapper = new MenuItemMapper();

    var json = """
    {
      "id": "combo-invalid",
      "displayName": "Invalid Combo",
      "kind": "combo",
      "category": { "id": "combo", "name": "Combos" },
      "price": { "amount": 20, "currency": "RON" },
      "allergens": [],
      "isAvailable": true,
      "parts": [
        {
          "id": "",
          "displayName": "",
          "kind": "food",
          "category": { "id": "pas", "name": "Pastries" },
          "price": { "amount": 8, "currency": "RON" },
          "allergens": [],
          "isAvailable": true
        }
      ]
    }
    """;

    Assert.Null(mapper.FromJson(json));
}
    
}