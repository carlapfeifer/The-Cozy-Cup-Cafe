using Capstone.Core;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class Lab05Tests
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
    public void RecentlyUsed_rejects_invalid_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentlyUsed<int>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentlyUsed<int>(-1));
    }
}
