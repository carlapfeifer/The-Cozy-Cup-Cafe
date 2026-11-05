using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Wiring;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class Lab06Tests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Pastries = new("pas", "Pastries");

    private static IItem Make(string id, string name) =>
        new Drink(id, name, Coffee, new Money(12.5m, "RON"));

    private static CafeStore SampleMenu()
    {
        var store = new CafeStore();
        store.Add(new Drink("c1", "Latte", Coffee, new Money(14m, "RON"), Size.Large));
        store.Add(new Drink("c2", "Mocha", Coffee, new Money(12m, "RON"), Size.Medium, Allergen.Milk,
            isAvailable: false));
        store.Add(new Food("p1", "Croissant", Pastries, new Money(8m, "RON"), Allergen.Gluten | Allergen.Milk));
        store.Add(new Food("p2", "Almond tart", Pastries, new Money(10m, "RON"), Allergen.Nuts));
        return store;
    }

    // ---- the event ----

    [Fact]
    public void ItemAdded_fires_after_the_item_is_in_the_store()
    {
        var store = new CafeStore();
        var counts = new List<int>();
        store.ItemAdded += _ => counts.Add(store.Count);

        store.Add(Make("a", "Latte"));
        store.Add(Make("b", "Mocha"));

        Assert.Equal(new[] { 1, 2 }, counts);
    }

    [Fact]
    public void Adding_with_no_subscribers_does_not_throw()
    {
        var store = new CafeStore();
        var ex = Record.Exception(() => store.Add(Make("a", "Latte")));
        Assert.Null(ex);
    }

    [Fact]
    public void A_rejected_duplicate_raises_nothing()
    {
        var store = new CafeStore();
        store.Add(Make("a", "Latte"));
        var heard = 0;
        store.ItemAdded += _ => heard++;

        Assert.Throws<ArgumentException>(() => store.Add(Make("a", "Other")));

        Assert.Equal(0, heard);
    }

    [Fact]
    public void Unsubscribe_detaches_only_that_handler()
    {
        var store = new CafeStore();
        var leaving = 0;
        var staying = 0;
        var stop = StoreFunctions.Subscribe(store, _ => leaving++);
        StoreFunctions.Subscribe(store, _ => staying++);

        store.Add(Make("a", "Latte"));
        stop();
        store.Add(Make("b", "Mocha"));

        Assert.Equal(1, leaving);
        Assert.Equal(2, staying);
    }

    [Fact]
    public void ItemRemoved_notifies_subscriber_after_item_is_removed()
    {
        var wiring = new Lab06Wiring();
        var store = new CafeStore();
        var item = Make("a", "Latte");
        IItem? removed = null;
        store.Add(item);
        wiring.SubscribeItemRemoved(store, removedItem =>
        {
            removed = removedItem;
            Assert.Null(store.FindById(removedItem.Id));
        });

        Assert.True(store.Remove("a"));

        Assert.Same(item, removed);
    }

    [Fact]
    public void ItemUpdated_notifies_subscriber_with_old_and_new_items()
    {
        var wiring = new Lab06Wiring();
        var store = new CafeStore();
        var original = Make("a", "Latte");
        var updated = Make("a", "Mocha");
        ItemUpdatedChange? change = null;
        store.Add(original);
        wiring.SubscribeItemUpdated(store, (_, update) => change = update);

        Assert.True(store.Update(updated));

        Assert.Same(updated, store.FindById("a"));
        Assert.Same(original, change?.PreviousItem);
        Assert.Same(updated, change?.UpdatedItem);
    }

    [Fact]
    public void Unsubscribing_from_item_events_stops_notifications()
    {
        var wiring = new Lab06Wiring();
        var store = new CafeStore();
        var removedCount = 0;
        var updatedCount = 0;
        var availabilityChangedCount = 0;
        var priceChangedCount = 0;
        store.Add(Make("a", "Latte"));
        var stopRemoved = wiring.SubscribeItemRemoved(store, _ => removedCount++);
        var stopUpdated = wiring.SubscribeItemUpdated(store, (_, _) => updatedCount++);
        var stopAvailability = wiring.SubscribeAvailabilityChanged(store, (_, _) => availabilityChangedCount++);
        var stopPrice = wiring.SubscribePriceChanged(store, (_, _) => priceChangedCount++);

        stopRemoved();
        stopUpdated();
        stopAvailability();
        stopPrice();
        store.Update(Make("a", "Mocha"));
        store.SetAvailability("a", false);
        store.SetPrice("a", new Money(13m, "RON"));
        store.Remove("a");

        Assert.Equal(0, removedCount);
        Assert.Equal(0, updatedCount);
        Assert.Equal(0, availabilityChangedCount);
        Assert.Equal(0, priceChangedCount);
    }

    [Fact]
    public void AvailabilityChanged_reports_the_transition_after_the_item_is_updated()
    {
        var store = new CafeStore();
        var original = new Drink("a", "Latte", Coffee, new Money(12.5m, "RON"), Size.Large, Allergen.Milk);
        AvailabilityChangedChange? change = null;
        store.Add(original);
        store.AvailabilityChanged += (_, update) =>
        {
            change = update;
            Assert.False(((MenuItem)store.FindById("a")!).IsAvailable);
        };

        Assert.True(store.SetAvailability("a", false));

        var updated = Assert.IsType<Drink>(store.FindById("a"));
        Assert.False(updated.IsAvailable);
        Assert.Equal(Size.Large, updated.Size);
        Assert.Equal(Allergen.Milk, updated.Allergens);
        Assert.Same(updated, change?.Item);
        Assert.True(change?.PreviousAvailability);
        Assert.False(change?.IsAvailable);
    }

    [Fact]
    public void PriceChanged_reports_old_and_new_prices_after_the_item_is_updated()
    {
        var store = new CafeStore();
        var original = new Food("a", "Croissant", Pastries, new Money(8m, "RON"), Allergen.Gluten);
        var newPrice = new Money(9m, "RON");
        PriceChangedChange? change = null;
        store.Add(original);
        store.PriceChanged += (_, update) =>
        {
            change = update;
            Assert.Equal(newPrice, ((MenuItem)store.FindById("a")!).Price);
        };

        Assert.True(store.SetPrice("a", newPrice));

        var updated = Assert.IsType<Food>(store.FindById("a"));
        Assert.Equal(newPrice, updated.Price);
        Assert.Equal(Allergen.Gluten, updated.Allergens);
        Assert.Same(updated, change?.Item);
        Assert.Equal(original.Price, change?.PreviousPrice);
        Assert.Equal(newPrice, change?.Price);
    }

    [Fact]
    public void Availability_and_price_changes_do_not_notify_for_missing_or_unchanged_values()
    {
        var store = new CafeStore();
        var item = Make("a", "Latte");
        var changes = 0;
        var itemPrice = new Money(12.5m, "RON");
        store.Add(item);
        store.AvailabilityChanged += (_, _) => changes++;
        store.PriceChanged += (_, _) => changes++;

        Assert.False(store.SetAvailability("missing", false));
        Assert.False(store.SetPrice("missing", new Money(13m, "RON")));
        Assert.True(store.SetAvailability("a", true));
        Assert.True(store.SetPrice("a", itemPrice));

        Assert.Equal(0, changes);
    }

    [Fact]
    public void Combo_cannot_be_marked_available_when_a_part_is_unavailable()
    {
        var unavailablePart = new Drink(
            "part",
            "Mocha",
            Coffee,
            new Money(12m, "RON"),
            isAvailable: false);
        var combo = new Combo(
            "combo",
            "Mocha combo",
            Coffee,
            new Money(15m, "RON"),
            new[] { unavailablePart });
        var store = new CafeStore();
        store.Add(combo);

        Assert.Throws<InvalidOperationException>(() => store.SetAvailability("combo", true));
        Assert.Same(combo, store.FindById("combo"));
        Assert.False(((MenuItem)store.FindById("combo")!).IsAvailable);
    }

    // ---- functions as parameters ----

    [Fact]
    public void Where_asks_once_per_item_and_keeps_store_order()
    {
        var store = new CafeStore();
        store.Add(Make("a", "Latte"));
        store.Add(Make("b", "Mocha"));
        store.Add(Make("c", "Lungo"));
        var asked = 0;

        var kept = StoreFunctions.Where(store, i =>
        {
            asked++;
            return i.DisplayName.StartsWith('L');
        });

        Assert.Equal(3, asked);
        Assert.Equal(new[] { "a", "c" }, kept.Select(i => i.Id));
    }

    [Fact]
    public void Map_returns_one_text_per_item_in_store_order()
    {
        var store = SampleMenu();

        var names = StoreFunctions.Map(store, i => i.DisplayName.ToUpperInvariant()).ToArray();

        Assert.Equal(new[] { "LATTE", "MOCHA", "CROISSANT", "ALMOND TART" }, names);
    }

    [Fact]
    public void A_null_function_is_a_programming_error()
    {
        var store = new CafeStore();
        Assert.Throws<ArgumentNullException>(() => StoreFunctions.Where(store, null!));
        Assert.Throws<ArgumentNullException>(() => StoreFunctions.Map(store, null!));
        Assert.Throws<ArgumentNullException>(() => StoreFunctions.ForEach(store, null!));
    }

    // ---- closures ----

    [Fact]
    public void ForEach_closure_adds_up_the_value_of_everything_on_sale()
    {
        var store = SampleMenu();
        var onSaleValue = 0m;

        StoreFunctions.ForEach(store, item =>
        {
            if (item is MenuItem menuItem && menuItem.IsAvailable)
                onSaleValue += menuItem.Price.Amount; // Mocha is sold out, so it is not counted
        });

        Assert.Equal(32m, onSaleValue); // 14 + 8 + 10
    }

    [Fact]
    public void A_lambda_sees_a_variable_that_changes_after_it_was_written()
    {
        var store = SampleMenu();
        var limit = 9m;
        Func<IItem, bool> withinLimit = item => item is MenuItem m && m.Price.Amount <= limit;

        var before = StoreFunctions.Where(store, withinLimit).Count; // Croissant only
        limit = 14m; // the lambda holds the variable, not a copy
        var after = StoreFunctions.Where(store, withinLimit).Count; // everything

        Assert.Equal(1, before);
        Assert.Equal(4, after);
    }

    // ---- MenuFilters: a function that returns a function ----

    [Fact]
    public void AtMostPrice_includes_the_limit_itself()
    {
        var kept = StoreFunctions.Where(SampleMenu(), MenuFilters.AtMostPrice(10m));

        Assert.Equal(new[] { "p1", "p2" }, kept.Select(i => i.Id));
    }
}