using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab06Wiring : ILab06Wiring
{
    public IItemStore CreateStore() => new CafeStore();

    public IItem CreateSample(int seed) =>
        new Drink(
            $"item-{seed}",
            $"Menu Item {seed}",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"));

    // The type that declares the ItemAdded event
    public Type PublisherType => typeof(CafeStore);

    public Action Subscribe(IItemStore store, Action<IItem> handler) =>
        StoreFunctions.Subscribe(store, handler);

    public Action SubscribeItemRemoved(IItemStore store, Action<IItem> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var cafeStore = RequireCafeStore(store);
        cafeStore.ItemRemoved += handler;
        return () => cafeStore.ItemRemoved -= handler;
    }

    public Action SubscribeItemUpdated(
        IItemStore store,
        EventHandler<ItemUpdatedChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var cafeStore = RequireCafeStore(store);
        cafeStore.ItemUpdated += handler;
        return () => cafeStore.ItemUpdated -= handler;
    }

    public Action SubscribeAvailabilityChanged(
        IItemStore store,
        EventHandler<AvailabilityChangedChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var cafeStore = RequireCafeStore(store);
        cafeStore.AvailabilityChanged += handler;
        return () => cafeStore.AvailabilityChanged -= handler;
    }

    public Action SubscribePriceChanged(
        IItemStore store,
        EventHandler<PriceChangedChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var cafeStore = RequireCafeStore(store);
        cafeStore.PriceChanged += handler;
        return () => cafeStore.PriceChanged -= handler;
    }

    public IReadOnlyList<IItem> Where(IItemStore store, Func<IItem, bool> keep) =>
        StoreFunctions.Where(store, keep);

    public IReadOnlyList<string> Map(IItemStore store, Func<IItem, string> describe) =>
        StoreFunctions.Map(store, describe);

    public void ForEach(IItemStore store, Action<IItem> action) =>
        StoreFunctions.ForEach(store, action);

    private static CafeStore RequireCafeStore(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store as CafeStore
               ?? throw new ArgumentException("Store is not a CafeStore.", nameof(store));
    }
}