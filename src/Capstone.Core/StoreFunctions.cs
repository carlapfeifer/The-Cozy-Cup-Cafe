using SharpQuest.Contracts;

namespace Capstone.Core;

public static class StoreFunctions
{
    public static IReadOnlyList<IItem> Where(IItemStore store, Func<IItem, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(keep);

        var kept = new List<IItem>();
        foreach (var item in store.All())
            if (keep(item))               // once per item; the caller decides, not us
                kept.Add(item);
        return kept;
    }

    public static IReadOnlyList<string> Map(IItemStore store, Func<IItem, string> describe)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(describe);

        var texts = new List<string>();
        foreach (var item in store.All())
            texts.Add(describe(item));
        return texts;
    }

    public static void ForEach(IItemStore store, Action<IItem> action)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in store.All())
            action(item);
    }

    /// <summary>Subscribes the handler and hands back the function that unsubscribes exactly it.</summary>
    public static Action Subscribe(IItemStore store, Action<IItem> handler)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(handler);

        var mine = store as CafeStore
                   ?? throw new ArgumentException("Store is not a CafeStore.", nameof(store));

        mine.ItemAdded += handler;
        return () => mine.ItemAdded -= handler;   // closure: still knows mine and handler
    }
}