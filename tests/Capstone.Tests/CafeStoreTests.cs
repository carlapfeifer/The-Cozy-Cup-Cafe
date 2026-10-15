using Capstone.Core;
using Capstone.Core.Models;

namespace Capstone.Tests;

public class CafeStoreTests
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Money Ten = new(10m, "RON");

    [Fact]
    public void Store_rejects_a_duplicate_id_and_keeps_one_item()
    {
        var store = new CafeStore();
        store.Add(new MenuItem("c1", "Latte", Coffee, Ten));

        Assert.Throws<ArgumentException>(
            () => store.Add(new MenuItem("c1", "Mocha", Coffee, Ten)));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Store_FindById_is_case_sensitive()
    {
        var store = new CafeStore();
        store.Add(new MenuItem("c1", "Latte", Coffee, Ten));

        Assert.Null(store.FindById("C1"));
        Assert.NotNull(store.FindById("c1"));
    }

    [Fact]
    public void Store_Remove_returns_false_for_unknown_id()
    {
        var store = new CafeStore();

        Assert.False(store.Remove("nope"), "removing a missing id should return false");
    }
}