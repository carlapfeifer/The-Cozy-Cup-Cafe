using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Wiring;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class Lab07Tests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private readonly Lab07Wiring _wiring = new();

    private static IItem Make(string id, string name) =>
        new Drink(id, name, Coffee, new Money(10m, "RON"));

    private static List<IItem> Items(params (string Id, string Name)[] values) =>
        values.Select(value => Make(value.Id, value.Name)).ToList();

    [Fact]
    public void Names_is_lazy_and_includes_items_added_after_the_query_is_created()
    {
        var store = new CafeStore();
        store.Add(Make("a", "Latte"));

        var names = _wiring.Names(store);
        store.Add(Make("b", "Mocha"));

        Assert.Equal(new[] { "Latte", "Mocha" }, names);
    }

    [Fact]
    public void Names_reflects_removals_and_can_be_enumerated_twice()
    {
        var store = new CafeStore();
        store.Add(Make("a", "Latte"));
        store.Add(Make("b", "Mocha"));

        var names = _wiring.Names(store);
        store.Remove("a");

        Assert.Equal(new[] { "Mocha" }, names);
        Assert.Equal(new[] { "Mocha" }, names);
    }

    [Fact]
    public void Sorted_ignores_case_and_breaks_name_ties_by_id()
    {
        var items = Items(
            ("z", "zebra"),
            ("m", "Mango"),
            ("b", "apple"),
            ("a", "APPLE"));

        var result = _wiring.Sorted(items);

        Assert.Equal(new[] { "a", "b", "m", "z" }, result.Select(item => item.Id));
        Assert.Equal(new[] { "z", "m", "b", "a" }, items.Select(item => item.Id));
    }

    [Fact]
    public void Page_orders_before_skipping_and_taking()
    {
        var items = Items(
            ("z", "zebra"),
            ("m", "Mango"),
            ("b", "apple"),
            ("a", "APPLE"));

        var result = _wiring.Page(items, skip: 1, take: 2);

        Assert.Equal(new[] { "b", "m" }, result.Select(item => item.Id));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public void Page_rejects_negative_arguments(int skip, int take)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _wiring.Page(Items(("a", "Latte")), skip, take));
    }

    [Fact]
    public void Longest_orders_by_name_length_then_id()
    {
        var items = Items(
            ("z", "cinnamon"),
            ("c", "croissant"),
            ("t", "tea"),
            ("a", "espresso"));

        Assert.Equal(
            new[] { "c", "a", "z" },
            _wiring.Longest(items, 3).Select(item => item.Id));

        Assert.Equal(
            new[] { "c", "a", "z" },
            items.Longest(3).Select(item => item.Id));
    }

    [Fact]
    public void Longest_rejects_negative_counts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _wiring.Longest(Items(("a", "Latte")), -1));
    }

    [Fact]
    public void Summary_counts_initials_and_calculates_name_statistics()
    {
        var items = Items(
            ("1", "Latte"),
            ("2", "latte"),
            ("3", "Bun"),
            ("4", "Croissant"));

        var summary = _wiring.Summary(items);

        Assert.Equal(4, summary.Count);
        Assert.Equal(3, summary.DistinctInitials);
        Assert.Equal("Croissant", summary.LongestName);
        Assert.Equal(5.5, summary.AverageNameLength);
    }

    [Fact]
    public void Summary_returns_zero_values_for_an_empty_sequence()
    {
        var summary = _wiring.Summary([]);

        Assert.Equal(new StoreStats(0, 0, null, 0), summary);
    }

    [Fact]
    public void Queries_reject_null_sequences()
    {
        Assert.Throws<ArgumentNullException>(() => _wiring.Names(null!));
        Assert.Throws<ArgumentNullException>(() => _wiring.Sorted(null!));
        Assert.Throws<ArgumentNullException>(() => _wiring.Page(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => _wiring.Longest(null!, 1));
        Assert.Throws<ArgumentNullException>(() => _wiring.Summary(null!));
    }
}