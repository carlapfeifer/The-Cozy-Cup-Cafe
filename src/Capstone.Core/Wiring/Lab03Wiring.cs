using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab03Wiring : ILab03Wiring
{
    public IItemStore CreateStore() => new CafeStore();

    public IItem CreateSample(int seed) =>
        new Drink(
            $"item-{seed}",
            $"Menu Item {seed}",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"));

    public IReadOnlyList<IItem> Search(IItemStore store, string query)
    {
        ArgumentNullException.ThrowIfNull(store);
        var matcher = SearchMatcher.Create();
        return store.All()
            .Where(item => matcher.Matches(item.DisplayName, query))
            .ToList();
    }
}