using Capstone.Core;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class CafeStoreTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Money Ten = new(10m, "RON");

    [Fact]
    public void Store_rejects_a_duplicate_id_and_keeps_one_item()
    {
        var store = new CafeStore();
        store.Add(new Drink("c1", "Latte", Coffee, Ten));

        Assert.Throws<ArgumentException>(
            () => store.Add(new Drink("c1", "Mocha", Coffee, Ten)));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Store_FindById_is_case_sensitive()
    {
        var store = new CafeStore();
        store.Add(new Drink("c1", "Latte", Coffee, Ten));

        Assert.Null(store.FindById("C1"));
        Assert.NotNull(store.FindById("c1"));
    }

    [Fact]
    public void Store_Remove_returns_false_for_unknown_id()
    {
        var store = new CafeStore();

        Assert.False(store.Remove("nope"), "removing a missing id should return false");
    }

    [Fact]
    public void Store_Remove_raises_event_after_removing_the_item()
    {
        var store = new CafeStore();
        var item = new Drink("c1", "Latte", Coffee, Ten);
        IItem? removed = null;
        store.Add(item);
        store.ItemRemoved += removedItem =>
        {
            removed = removedItem;
            Assert.Null(store.FindById(removedItem.Id));
        };

        Assert.True(store.Remove("c1"));

        Assert.Same(item, removed);
    }

    [Fact]
    public void Store_Update_replaces_item_and_raises_event_with_both_items()
    {
        var store = new CafeStore();
        var original = new Drink("c1", "Latte", Coffee, Ten);
        var updated = new Drink("c1", "Mocha", Coffee, Ten);
        ItemUpdatedChange? change = null;
        store.Add(original);
        store.ItemUpdated += (_, update) => change = update;

        Assert.True(store.Update(updated));

        Assert.Same(updated, store.FindById("c1"));
        Assert.Same(updated, store[0]);
        Assert.Same(original, change?.PreviousItem);
        Assert.Same(updated, change?.UpdatedItem);
    }

    [Fact]
    public void Store_Update_returns_false_for_unknown_id_without_raising_event()
    {
        var store = new CafeStore();
        var eventRaised = false;
        store.ItemUpdated += (_, _) => eventRaised = true;

        Assert.False(store.Update(new Drink("missing", "Mocha", Coffee, Ten)));
        Assert.False(eventRaised);
    }
}