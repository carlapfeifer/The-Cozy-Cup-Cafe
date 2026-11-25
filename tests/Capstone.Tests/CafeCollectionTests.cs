using Capstone.Core;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class CafeCollectionTests
{
    private static IItem MakeItem(string id, string name) =>
        new Drink(id, name, new Category("cof", "Coffee"), new Money(12.5m, "RON"));

    [Fact]
    public void CafeStore_enumerates_in_insertion_order()
    {
        var store = new CafeStore();
        var first = MakeItem("c1", "Cappuccino");
        var second = MakeItem("c2", "Latte");
        var third = MakeItem("c3", "Mocha");

        store.Add(first);
        store.Add(second);
        store.Add(third);

        var seen = new List<string>();
        foreach (var item in store)
        {
            seen.Add(item.Id);
        }

        Assert.Equal(new[] { "c1", "c2", "c3" }, seen);
    }

    [Fact]
    public void CafeStore_indexers_obey_position_and_id()
    {
        var store = new CafeStore();
        var first = MakeItem("tea", "Green Tea");
        var second = MakeItem("cold", "Iced Tea");

        store.Add(first);
        store.Add(second);

        Assert.Same(first, store[0]);
        Assert.Same(second, store[1]);
        Assert.Same(second, store["cold"]);
        Assert.Null(store["missing"]);
    }

    [Fact]
    public void CafeStore_matching_is_case_insensitive_and_preserves_order()
    {
        var store = new CafeStore();
        store.Add(MakeItem("m1", "Cappuccino"));
        store.Add(MakeItem("m2", "Mocha"));
        store.Add(MakeItem("m3", "Iced Tea"));

        var matches = store.Matching("tea").Select(item => item.Id).ToList();

        Assert.Equal(new[] { "m3" }, matches);

        var caseInsensitiveMatches = store.Matching("CAPPUCCINO").Select(item => item.Id).ToList();
        Assert.Equal(new[] { "m1" }, caseInsensitiveMatches);
    }

    [Fact]
    public void RecentlyUsed_keeps_the_most_recent_items_first()
    {
        var recent = new RecentlyUsed<string>(3);

        recent.Push("a");
        recent.Push("b");
        recent.Push("c");

        Assert.Equal(new[] { "c", "b", "a" }, recent.Items());
    }

    [Fact]
    public void RecentlyUsed_drops_oldest_and_moves_duplicates_to_the_front()
    {
        var recent = new RecentlyUsed<string>(3);

        recent.Push("a");
        recent.Push("b");
        recent.Push("a");
        recent.Push("c");
        recent.Push("d");

        Assert.Equal(new[] { "d", "c", "a" }, recent.Items());

        var repeat = new RecentlyUsed<int>(2);
        repeat.Push(1);
        repeat.Push(2);
        repeat.Push(1);

        Assert.Equal(new[] { 1, 2 }, repeat.Items());
    }

    [Fact]
    public void RecentlyUsed_replaces_an_existing_value_without_changing_its_order()
    {
        var recent = new RecentlyUsed<string>(3);
        recent.Push("croissant");
        recent.Push("pistachio roll");
        recent.Push("mocha");

        Assert.True(recent.Replace("croissant", "butter croissant"));

        Assert.Equal(new[] { "mocha", "pistachio roll", "butter croissant" }, recent.Items());
        Assert.False(recent.Replace("missing", "new value"));
    }

    [Fact]
    public void RecentlyUsed_rejects_invalid_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentlyUsed<int>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentlyUsed<int>(-1));
    }
    
    
    
    [Fact]
public void CafeStore_first_last_and_available_items_reflect_the_menu()
{
    var store = new CafeStore();

    Assert.Null(store.First);
    Assert.Null(store.Last);

    var first = MakeItem("available", "Latte");
    var unavailable = new Drink(
        "unavailable",
        "Mocha",
        new Category("cof", "Coffee"),
        new Money(12.5m, "RON"),
        isAvailable: false);

    store.Add(first);
    store.Add(unavailable);

    Assert.Same(first, store.First);
    Assert.Same(unavailable, store.Last);
    Assert.Equal(1, store.AvailableCount);
    Assert.Equal(new[] { "available" }, store.AvailableItems.Select(item => item.Id));

    store.SetAvailability("available", false);

    Assert.Equal(0, store.AvailableCount);
    Assert.Empty(store.AvailableItems);
}

[Fact]
public void CafeStore_category_index_tracks_add_remove_and_update_in_menu_order()
{
    var store = new CafeStore();
    var coffee1 = MakeItem("coffee-1", "Latte");
    var tea = new Drink(
        "tea-1",
        "Green Tea",
        new Category("tea", "Tea"),
        new Money(8m, "RON"));
    var coffee2 = MakeItem("coffee-2", "Mocha");

    store.Add(coffee1);
    store.Add(tea);
    store.Add(coffee2);

    Assert.True(store.ContainsCategory("COF"));
    Assert.Equal(2, store.CountByCategory("cof"));
    Assert.Equal(
        new[] { "coffee-1", "coffee-2" },
        store[new Category("cof", "Coffee")].Select(item => item.Id));

    var movedCoffee = new Drink(
        "coffee-2",
        "Mocha",
        new Category("tea", "Tea"),
        new Money(12.5m, "RON"));

    Assert.True(store.Update(movedCoffee));
    Assert.Equal(new[] { "coffee-1" }, store[new Category("cof", "Coffee")].Select(item => item.Id));
    Assert.Equal(
        new[] { "tea-1", "coffee-2" },
        store[new Category("tea", "Tea")].Select(item => item.Id));

    Assert.True(store.Remove("coffee-1"));
    Assert.False(store.ContainsCategory("cof"));
}

[Fact]
public void CafeStore_supports_linq_slicing_in_insertion_order()
{
    var store = new CafeStore();
    store.Add(MakeItem("one", "One"));
    store.Add(MakeItem("two", "Two"));
    store.Add(MakeItem("three", "Three"));
    store.Add(MakeItem("four", "Four"));

    var page = store.Skip(1).Take(2).Select(item => item.Id);

    Assert.Equal(new[] { "two", "three" }, page);
}

[Fact]
public void CompletedOrderHistory_keeps_recent_orders_newest_first_and_within_capacity()
{
    var history = new CompletedOrderHistory(capacity: 2);
    var first = new Order();
    var second = new Order();
    var third = new Order();

    history.Record(first);
    history.Record(second);
    history.Record(third);

    Assert.Equal(2, history.Count);
    Assert.Same(third, history.Orders[0]);
    Assert.Same(second, history.Orders[1]);
}
[Fact]
public void RecentlyUsed_items_returns_a_snapshot()
{
    var recent = new RecentlyUsed<string>(2);
    recent.Push("first");

    var snapshot = recent.Items().ToArray();
    recent.Push("second");

    Assert.Equal(new[] { "first" }, snapshot);
    Assert.Equal(new[] { "second", "first" }, recent.Items());
}
}
