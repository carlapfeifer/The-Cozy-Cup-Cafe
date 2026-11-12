using System.Linq;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab07Wiring : ILab07Wiring
{
    public IItemStore CreateStore() => new CafeStore();

    public IItem CreateSample(int seed) =>
        new Drink(
            $"item-{seed}",
            $"Menu Item {seed}",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"));

    public IEnumerable<string> Names(IItemStore store)
    {
        var cafeStore = RequireCafeStore(store);
        return cafeStore.Select(item => item.DisplayName);
    }

    public IReadOnlyList<IItem> Sorted(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<IItem> Page(IEnumerable<IItem> items, int skip, int take)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        return items
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<IItem> Longest(IEnumerable<IItem> items, int n) =>
        items.Longest(n).ToList();

    public Type ExtensionHost => typeof(ItemQueries);

    public StoreStats Summary(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var snapshot = items.ToList();

        if (snapshot.Count == 0)
            return new StoreStats(0, 0, null, 0);

        return new StoreStats(
            snapshot.Count(),
            snapshot
                .Select(item => item.DisplayName[..1])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            snapshot.Longest(1).Single().DisplayName,
            snapshot.Select(item => item.DisplayName.Length).Average());
    }

    private static CafeStore RequireCafeStore(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        return store as CafeStore
               ?? throw new ArgumentException("Store is not a CafeStore.", nameof(store));
    }
}