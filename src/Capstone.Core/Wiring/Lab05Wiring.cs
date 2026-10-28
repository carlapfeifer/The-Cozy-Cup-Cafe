using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab05Wiring : ILab05Wiring
{
    public IItemStore CreateStore() => new CafeStore();

    // Sample item using decimal price (never double)
    public IItem CreateSample(int seed) =>
        new Drink(
            $"item-{seed}",
            $"Menu Item {seed}",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"));

    // Return the concrete class of your store
    public Type CollectionType => typeof(CafeStore);

    // Open generic definition with empty angle brackets <>
    public Type GenericDefinition => typeof(RecentlyUsed<>);

    public IItem At(IItemStore store, int index)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (store is CafeStore cafeStore)
        {
            return cafeStore[index];
        }
        throw new ArgumentException("Store is not a CafeStore.", nameof(store));
    }

    public IItem? ById(IItemStore store, string id)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (store is CafeStore cafeStore)
        {
            return cafeStore[id];
        }
        return store.FindById(id);
    }

    public IReadOnlyList<IItem> EachInOrder(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (store is IEnumerable<IItem> enumerable)
        {
            var list = new List<IItem>();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }
            return list;
        }
        throw new ArgumentException("Store does not implement IEnumerable<IItem>.", nameof(store));
    }

    public IEnumerable<IItem> Matching(IItemStore store, string text)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (store is CafeStore cafeStore)
        {
            return cafeStore.Matching(text);
        }

        return MatchingFallback(store, text);
    }

    private static IEnumerable<IItem> MatchingFallback(IItemStore store, string text)
    {
        if (store is IEnumerable<IItem> items)
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(text) || item.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    yield return item;
                }
            }
        }
    }

    public IReadOnlyList<T> Recent<T>(int capacity, params T[] pushed)
    {
        var recent = new RecentlyUsed<T>(capacity);
        foreach (var item in pushed)
        {
            recent.Push(item);
        }
        return recent.Items().ToArray();
        
    }
}
